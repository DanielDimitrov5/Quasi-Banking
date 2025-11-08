using System.Text.Json;
using System.Text.Json.Serialization;
using Confluent.Kafka;
using AccountService.Data;
using AccountService.Models;
using Microsoft.EntityFrameworkCore;

namespace AccountService.Services;

public class KafkaConsumerService : BackgroundService
{
    private IConsumer<string, string>? _consumer;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<KafkaConsumerService> _logger;
    private readonly IConfiguration _configuration;
    private readonly JsonSerializerOptions _jsonOptions;

    public KafkaConsumerService(
        IConfiguration configuration,
        IServiceProvider serviceProvider,
        ILogger<KafkaConsumerService> logger)
    {
        _configuration = configuration;
        _serviceProvider = serviceProvider;
        _logger = logger;

        _jsonOptions = new JsonSerializerOptions
        {
            Converters = { new JsonStringEnumConverter() },
            PropertyNamingPolicy = null,
            PropertyNameCaseInsensitive = true
        };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Add delay to let the app start first
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        try
        {
            var config = new ConsumerConfig
            {
                BootstrapServers = _configuration["Kafka:BootstrapServers"],
                GroupId = _configuration["Kafka:GroupId"],
                AutoOffsetReset = AutoOffsetReset.Latest,
                EnableAutoCommit = false,
                SessionTimeoutMs = 10000,
                HeartbeatIntervalMs = 3000
            };

            _consumer = new ConsumerBuilder<string, string>(config).Build();
            _consumer.Subscribe("banking-events");
            
            _logger.LogInformation("🎧 Account Service: Kafka consumer started successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to initialize Kafka consumer. Will retry...");
            
            // Don't throw - let the service continue running
            // The consumer will retry connection
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            
            // Retry initialization
            try
            {
                var config = new ConsumerConfig
                {
                    BootstrapServers = _configuration["Kafka:BootstrapServers"],
                    GroupId = _configuration["Kafka:GroupId"],
                    AutoOffsetReset = AutoOffsetReset.Latest,
                    EnableAutoCommit = false
                };

                _consumer = new ConsumerBuilder<string, string>(config).Build();
                _consumer.Subscribe("banking-events");
                _logger.LogInformation("🎧 Kafka consumer connected on retry");
            }
            catch (Exception retryEx)
            {
                _logger.LogError(retryEx, "❌ Kafka consumer failed to start after retry. Service will continue without consumer.");
                return; // Exit gracefully without crashing the app
            }
        }

        if (_consumer == null)
        {
            _logger.LogWarning("⚠️ Kafka consumer not initialized. Exiting consumer service.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var consumeResult = _consumer.Consume(TimeSpan.FromSeconds(1));
                
                if (consumeResult == null)
                    continue;
                
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AccountDbContext>();

                try
                {
                    var eventData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
                        consumeResult.Message.Value, _jsonOptions);

                    if (eventData == null || !eventData.ContainsKey("EventType"))
                    {
                        _logger.LogWarning("⚠️ Skipping message with no EventType");
                        _consumer.Commit(consumeResult);
                        continue;
                    }

                    var eventType = eventData["EventType"].GetString();
                    _logger.LogInformation($"📨 Received event: {eventType}");

                    if (eventType == "TransactionProcessedEvent")
                    {
                        var transactionEvent = JsonSerializer.Deserialize<TransactionProcessedEventDto>(
                            consumeResult.Message.Value, _jsonOptions);

                        if (transactionEvent != null)
                        {
                            await UpdateAccountBalance(dbContext, transactionEvent);
                        }
                    }

                    _consumer.Commit(consumeResult);
                }
                catch (JsonException jsonEx)
                {
                    _logger.LogWarning(jsonEx, 
                        $"⚠️ Skipping malformed message at offset {consumeResult.Offset}");
                    _consumer.Commit(consumeResult);
                }
            }
            catch (ConsumeException ex)
            {
                _logger.LogError(ex, "❌ Kafka consume error");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error processing event");
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
        }
    }

    private async Task UpdateAccountBalance(
        AccountDbContext dbContext, 
        TransactionProcessedEventDto transactionEvent)
    {
        var strategy = dbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await dbContext.Database.BeginTransactionAsync();
            
            try
            {
                var account = await dbContext.Accounts
                    .FirstOrDefaultAsync(a => a.Id == transactionEvent.AccountId);

                if (account == null)
                {
                    _logger.LogWarning($"⚠️ Account {transactionEvent.AccountId} not found");
                    return;
                }

                var previousBalance = account.Balance;
                account.Balance += transactionEvent.Amount;
                account.UpdatedAt = DateTime.UtcNow;

                await dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation(
                    $"✅ Updated account {account.Id} balance: {previousBalance:C} → {account.Balance:C} (Change: {transactionEvent.Amount:C})");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, $"❌ Failed to update balance for account {transactionEvent.AccountId}");
                throw;
            }
        });
    }

    public override void Dispose()
    {
        _consumer?.Close();
        _consumer?.Dispose();
        base.Dispose();
    }
}

public class TransactionProcessedEventDto
{
    public string TransactionId { get; set; } = null!;
    public string AccountId { get; set; } = null!;
    public decimal Amount { get; set; }
    public TransactionTypeDto Type { get; set; }
    public string Description { get; set; } = null!;
}

public enum TransactionTypeDto
{
    Deposit,
    Withdrawal
}
