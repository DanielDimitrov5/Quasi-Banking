namespace AccountService.Services;

public interface IKafkaProducerService
{
    Task PublishEventAsync(string topic, string key, string jsonPayload);
}
