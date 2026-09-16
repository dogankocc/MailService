using System.Text.Json;
using Confluent.Kafka;
using MailService.Data;
using MailService.Models;
using MailService.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;

namespace MailService.Workers;

public class KafkaMailWorker : BackgroundService
{
    private readonly KafkaSettings _settings;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<KafkaMailWorker> _logger;
    private readonly AsyncRetryPolicy<bool> _mailRetryPolicy;

    public KafkaMailWorker(IOptions<KafkaSettings> settings, IServiceProvider serviceProvider, ILogger<KafkaMailWorker> logger)
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

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await ConsumeLoop(stoppingToken);
    }

    private async Task ConsumeLoop(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _settings.BootstrapServers,
            GroupId = _settings.ConsumerGroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true,
            AutoCommitIntervalMs = 5000
        };

        using var consumer = new ConsumerBuilder<Ignore, string>(config).Build();
        consumer.Subscribe(_settings.Topic);

        _logger.LogInformation("Kafka Mail Worker started. Listening to topic: {Topic}, Group: {GroupId}",
            _settings.Topic, _settings.ConsumerGroupId);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = consumer.Consume(stoppingToken);
                if (result == null)
                    continue;

                _logger.LogInformation("Kafka message received: Partition {Partition}, Offset {Offset}",
                    result.Partition, result.Offset);

                var message = JsonSerializer.Deserialize<MailQueueMessage>(result.Message.Value);
                if (message != null)
                {
                    await ProcessMessageAsync(message, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (ConsumeException ex)
            {
                _logger.LogError(ex, "Kafka consume error");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kafka worker error");
                await Task.Delay(5000, stoppingToken);
            }
        }

        _logger.LogInformation("Kafka Mail Worker stopped");
    }

    private async Task ProcessMessageAsync(MailQueueMessage message, CancellationToken stoppingToken)
    {
        _logger.LogInformation("Processing mail {RequestId} for {Count} recipients",
            message.RequestId, message.To.Count);

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MailServiceDbContext>();
        var mailSender = scope.ServiceProvider.GetRequiredService<IMailSender>();

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
}
