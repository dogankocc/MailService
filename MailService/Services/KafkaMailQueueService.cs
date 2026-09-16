using System.Text.Json;
using Confluent.Kafka;
using MailService.Models;
using Microsoft.Extensions.Options;

namespace MailService.Services;

public class KafkaMailQueueService : IMailQueueService
{
    private readonly KafkaSettings _settings;
    private readonly ILogger<KafkaMailQueueService> _logger;

    public KafkaMailQueueService(IOptions<KafkaSettings> settings, ILogger<KafkaMailQueueService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task EnqueueAsync(MailQueueMessage message)
    {
        var config = new ProducerConfig
        {
            BootstrapServers = _settings.BootstrapServers,
            Acks = Acks.Leader,
            RetryBackoffMs = 1000,
            MessageTimeoutMs = 30000
        };

        using var producer = new ProducerBuilder<Null, string>(config).Build();

        var json = JsonSerializer.Serialize(message);
        var result = await producer.ProduceAsync(
            _settings.Topic,
            new Message<Null, string> { Value = json });

        if (result.Status == PersistenceStatus.Persisted)
        {
            _logger.LogInformation("Mail queued via Kafka: {RequestId}, Partition: {Partition}, Offset: {Offset}",
                message.RequestId, result.Partition, result.Offset);
        }
        else
        {
            _logger.LogWarning("Mail not persisted to Kafka: {RequestId}, Status: {Status}",
                message.RequestId, result.Status);
        }
    }
}

public class KafkaSettings
{
    public string BootstrapServers { get; set; } = "localhost:9092";
    public string Topic { get; set; } = "mail_queue";
    public string ConsumerGroupId { get; set; } = "mail-service-group";
}
