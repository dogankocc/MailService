using System.Text;
using System.Text.Json;
using MailService.Data;
using MailService.Models;
using MailService.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MailService.Workers;

public class MailWorker : BackgroundService
{
    private readonly RabbitMqSettings _settings;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MailWorker> _logger;
    private readonly AsyncRetryPolicy<bool> _mailRetryPolicy;
    private IConnection? _connection;
    private IModel? _channel;

    public MailWorker(IOptions<RabbitMqSettings> settings, IServiceProvider serviceProvider, ILogger<MailWorker> logger)
    {
        _settings = settings.Value;
        _serviceProvider = serviceProvider;
        _logger = logger;

        // Polly Retry Policy: 3 deneme, üstel bekleme (2sn, 5sn, 10sn)
        _mailRetryPolicy = Policy
            .HandleResult<bool>(success => !success) // SendAsync false dönerse
            .Or<Exception>() // Herhangi bir istisna olursa
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: (attempt, _) => attempt switch
                {
                    1 => TimeSpan.FromSeconds(2),
                    2 => TimeSpan.FromSeconds(5),
                    _ => TimeSpan.FromSeconds(10)
                },
                onRetryAsync: (outcome, timeSpan, attempt, _) =>
                {
                    _logger.LogWarning(
                        outcome.Exception,
                        "Mail failed - Retry {Attempt} of 3. Waiting {Delay}s before next attempt",
                        attempt,
                        timeSpan.TotalSeconds);
                    return Task.CompletedTask;
                });
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _settings.HostName,
            UserName = _settings.UserName,
            Password = _settings.Password,
            Port = _settings.Port,
            DispatchConsumersAsync = true
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        _channel.QueueDeclare(
            queue: _settings.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);

            try
            {
                var mailMessage = JsonSerializer.Deserialize<MailQueueMessage>(message);
                if (mailMessage != null)
                {
                    await ProcessMessageAsync(mailMessage, stoppingToken);
                }
                _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing mail message");
                // Polly ile 3 deneme sonrası hala başarısızsa kuyruktan çıkar
                // BasicNack requeue=false ile mesajı kuyruktan tamamen çıkar
                _channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: false);
            }
        };

        _channel.BasicConsume(
            queue: _settings.QueueName,
            autoAck: false,
            consumer: consumer);

        _logger.LogInformation("Mail Worker (RabbitMQ) started. Listening to queue: {QueueName}", _settings.QueueName);
        return Task.CompletedTask;
    }

    private async Task ProcessMessageAsync(MailQueueMessage message, CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MailServiceDbContext>();
        var mailSender = scope.ServiceProvider.GetRequiredService<IMailSender>();

        // Body ve Subject zaten render edilmiş olarak geliyor (API'den)
        foreach (var to in message.To)
        {
            bool success;

            try
            {
                // Polly policy ile çalıştır
                var policyResult = await _mailRetryPolicy.ExecuteAndCaptureAsync(async ct =>
                    await mailSender.SendAsync(to, message.Subject, message.Body, message.Body),
                    stoppingToken);

                success = policyResult.Outcome == OutcomeType.Successful && policyResult.Result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception sending mail to {To}", to);
                success = false;
            }

            await UpdateSingleLogStatusAsync(dbContext, message.RequestId, to, success ? 1 : 0);

            if (!success)
            {
                _logger.LogError("Mail permanently FAILED for {To} (RequestId: {RequestId}) after 3 retries",
                    to, message.RequestId);
            }
            else
            {
                _logger.LogInformation("Mail SUCCESS for {To} (RequestId: {RequestId})",
                    to, message.RequestId);
            }
        }
    }

    private static async Task UpdateSingleLogStatusAsync(MailServiceDbContext dbContext, Guid requestId, string to, int status)
    {
        var log = await dbContext.MailLogs
            .FirstOrDefaultAsync(l => l.RequestId == requestId && l.To == to);

        if (log != null)
        {
            log.Status = status;
            log.SentDate = DateTime.UtcNow;
            await dbContext.SaveChangesAsync();
        }
    }

    public override void Dispose()
    {
        _channel?.Close();
        _channel?.Dispose();
        _connection?.Close();
        _connection?.Dispose();
        base.Dispose();
    }
}
