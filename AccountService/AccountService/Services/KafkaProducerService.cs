using Confluent.Kafka;

namespace AccountService.Services;

// Service to produce Kafka messages for account events
public class KafkaProducerService : IKafkaProducerService, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaProducerService> _logger;

    public KafkaProducerService(IConfiguration configuration, ILogger<KafkaProducerService> logger)
    {
        _logger = logger;
        var config = new ProducerConfig
        {
            BootstrapServers = configuration["Kafka:BootstrapServers"],
            Acks = Acks.All,
            EnableIdempotence = true
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task PublishEventAsync(string topic, string key, string jsonPayload)
    {
        try
        {
            var message = new Message<string, string>
            {
                Key = key,
                Value = jsonPayload
            };

            var result = await _producer.ProduceAsync(topic, message);
            _logger.LogInformation($"Event published to {result.TopicPartitionOffset}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing event to Kafka");
            throw;
        }
    }

    public void Dispose()
    {
        _producer?.Flush();
        _producer?.Dispose();
    }
}
