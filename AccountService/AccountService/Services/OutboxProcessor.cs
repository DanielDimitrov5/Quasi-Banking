using AccountService.Data;
using AccountService.Models;
using Microsoft.EntityFrameworkCore;

namespace AccountService.Services;

public class OutboxProcessor : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxProcessor> _logger;
    private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(5);
    private readonly int _batchSize = 100;
    private readonly int _maxRetries = 5;

    public OutboxProcessor(
        IServiceProvider serviceProvider,
        ILogger<OutboxProcessor> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox Processor started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxMessages(stoppingToken);
                await Task.Delay(_pollingInterval, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in outbox processor");
            }
        }
    }

    private async Task ProcessOutboxMessages(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AccountDbContext>();
        var kafkaProducer = scope.ServiceProvider.GetRequiredService<IKafkaProducerService>();

        var pendingMessages = await dbContext.OutboxMessages
            .Where(m => m.Status == OutboxStatus.Pending && m.RetryCount < _maxRetries)
            .OrderBy(m => m.CreatedAt)
            .Take(_batchSize)
            .ToListAsync(cancellationToken);

        if (!pendingMessages.Any())
            return;

        _logger.LogInformation($"Processing {pendingMessages.Count} outbox messages");

        foreach (var message in pendingMessages)
        {
            try
            {
                message.Status = OutboxStatus.Processing;
                await dbContext.SaveChangesAsync(cancellationToken);

                await kafkaProducer.PublishEventAsync(
                    "banking-events",
                    message.AggregateId,
                    message.Payload
                );

                message.Status = OutboxStatus.Completed;
                message.ProcessedAt = DateTime.UtcNow;
                
                _logger.LogInformation($"Published message {message.Id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to publish message {message.Id}");
                
                message.RetryCount++;
                message.Error = ex.Message;
                message.Status = message.RetryCount >= _maxRetries 
                    ? OutboxStatus.Failed 
                    : OutboxStatus.Pending;
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
