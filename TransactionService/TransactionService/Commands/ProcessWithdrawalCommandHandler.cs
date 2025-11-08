using System.Text.Json;
using System.Text.Json.Serialization;
using MediatR;
using TransactionService.Data;
using TransactionService.Models;
using TransactionService.Services;

namespace TransactionService.Commands;

public class ProcessWithdrawalCommandHandler : IRequestHandler<ProcessWithdrawalCommand, TransactionResponse>
{
    private readonly TransactionDbContext _dbContext;
    private readonly IEventStore _eventStore;
    private readonly IAccountServiceClient _accountServiceClient;
    private readonly ILogger<ProcessWithdrawalCommandHandler> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public ProcessWithdrawalCommandHandler(
        TransactionDbContext dbContext,
        IEventStore eventStore,
        IAccountServiceClient accountServiceClient,
        ILogger<ProcessWithdrawalCommandHandler> logger)
    {
        _dbContext = dbContext;
        _eventStore = eventStore;
        _accountServiceClient = accountServiceClient;
        _logger = logger;
        
        _jsonOptions = new JsonSerializerOptions
        {
            Converters = { new JsonStringEnumConverter() },
            PropertyNamingPolicy = null
        };
    }

    public async Task<TransactionResponse> Handle(ProcessWithdrawalCommand request, CancellationToken cancellationToken)
    {
        // ===== VALIDATION =====
        
        // 1. Validate amount
        if (request.Amount <= 0)
        {
            throw new ValidationException("Withdrawal amount must be greater than zero", "INVALID_AMOUNT");
        }

        if (request.Amount > 10000)
        {
            throw new ValidationException("Withdrawal amount exceeds daily limit of $10,000", "DAILY_LIMIT_EXCEEDED");
        }

        // 2. Validate account exists
        _logger.LogInformation($"Validating account {request.AccountId}...");
        var account = await _accountServiceClient.GetAccountAsync(request.AccountId);
        
        if (account == null)
        {
            throw new AccountNotFoundException(request.AccountId);
        }

        // 3. Validate sufficient balance
        if (account.Balance < request.Amount)
        {
            throw new InsufficientFundsException(account.Balance, request.Amount);
        }

        _logger.LogInformation($"✅ Account validated. Balance: {account.Balance:C}, Withdrawal: {request.Amount:C}");

        using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        
        try
        {
            var txn = new Transaction
            {
                Id = Guid.NewGuid().ToString(),
                AccountId = request.AccountId,
                Amount = -request.Amount,
                Type = TransactionType.Withdrawal,
                Description = request.Description,
                Status = TransactionStatus.Completed,
                CreatedAt = DateTime.UtcNow
            };

            var @event = new TransactionProcessedEvent
            {
                TransactionId = txn.Id,
                AccountId = txn.AccountId,
                Amount = -request.Amount,
                Type = TransactionType.Withdrawal,
                Description = txn.Description
            };

            await _eventStore.SaveEventAsync(@event, txn.Id);
            _dbContext.Transactions.Add(txn);

            var outboxMessage = new OutboxMessage
            {
                EventType = @event.EventType,
                AggregateId = txn.AccountId,
                Payload = JsonSerializer.Serialize(@event, _jsonOptions),
                Status = OutboxStatus.Pending
            };
            _dbContext.OutboxMessages.Add(outboxMessage);

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation($"✅ Withdrawal processed: {request.Amount:C} from account {txn.AccountId}");

            return new TransactionResponse(
                txn.Id,
                txn.AccountId,
                txn.Amount,
                txn.Type,
                txn.Description,
                txn.Status,
                txn.CreatedAt
            );
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
