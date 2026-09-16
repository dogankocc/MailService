using System.Text;
using System.Text.Json;
using MailService.Models;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace MailService.Services;

public class RabbitMqMailQueueService : IMailQueueService
{
    private readonly RabbitMqSettings _settings;
    private readonly ILogger<RabbitMqMailQueueService> _logger;

    public RabbitMqMailQueueService(IOptions<RabbitMqSettings> settings, ILogger<RabbitMqMailQueueService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public Task EnqueueAsync(MailQueueMessage message)
    {
        var factory = new ConnectionFactory
        {
            HostName = _settings.HostName,
            UserName = _settings.UserName,
            Password = _settings.Password,
            Port = _settings.Port
        };

        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        channel.QueueDeclare(
            queue: _settings.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        var json = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(json);

        var properties = channel.CreateBasicProperties();
        properties.Persistent = true;

        channel.BasicPublish(
            exchange: string.Empty,
            routingKey: _settings.QueueName,
            basicProperties: properties,
            body: body);

        _logger.LogInformation("Mail queued: {RequestId}", message.RequestId);
        return Task.CompletedTask;
    }
}

public class RabbitMqSettings
{
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string QueueName { get; set; } = "mail_queue";
}
