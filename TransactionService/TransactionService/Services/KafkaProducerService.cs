using Confluent.Kafka;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TransactionService.Services;

public class KafkaProducerService : IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaProducerService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public KafkaProducerService(IConfiguration configuration, ILogger<KafkaProducerService> logger)
    {
        _logger = logger;
        
        // Configure JSON to serialize enums as strings
        _jsonOptions = new JsonSerializerOptions
        {
            Converters = { new JsonStringEnumConverter() },
            PropertyNamingPolicy = null
        };
        
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
