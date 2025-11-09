using System.Text.Json;
using System.Text.Json.Serialization;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TransactionService.Data;
using TransactionService.Models;
using TransactionService.Services;

namespace TransactionService.Commands;

public class ProcessDepositCommandHandler : IRequestHandler<ProcessDepositCommand, TransactionResponse>
{
    private readonly TransactionDbContext _dbContext;
    private readonly IEventStore _eventStore;
    private readonly IAccountServiceClient _accountServiceClient;
    private readonly ILogger<ProcessDepositCommandHandler> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public ProcessDepositCommandHandler(
        TransactionDbContext dbContext,
        IEventStore eventStore,
        IAccountServiceClient accountServiceClient,
        ILogger<ProcessDepositCommandHandler> logger)
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

    public async Task<TransactionResponse> Handle(ProcessDepositCommand request, CancellationToken cancellationToken)
    {
        // ===== VALIDATION =====

        // 1. Validate amount
        if (request.Amount <= 0)
        {
            throw new ValidationException("Deposit amount must be greater than zero", "INVALID_AMOUNT");
        }

        if (request.Amount > 1000000)
        {
            throw new ValidationException("Deposit amount exceeds maximum limit of $1,000,000", "AMOUNT_TOO_LARGE");
        }

        // 2. Validate account exists
        _logger.LogInformation($"Validating account {request.AccountId}...");
        var account = await _accountServiceClient.GetAccountAsync(request.AccountId);

        if (account == null)
        {
            throw new AccountNotFoundException(request.AccountId);
        }

        _logger.LogInformation($"Account validated. Owner: {account.OwnerEmail}, Balance: {account.Balance:C}");

        // ===== PROCESS TRANSACTION (SIMPLE VERSION WITHOUT RETRY) =====

        using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var txn = new Transaction
            {
                Id = Guid.NewGuid().ToString(),
                AccountId = request.AccountId,
                Amount = request.Amount,
                Type = TransactionType.Deposit,
                Description = request.Description,
                Status = TransactionStatus.Completed,
                CreatedAt = DateTime.UtcNow
            };

            var @event = new TransactionProcessedEvent
            {
                TransactionId = txn.Id,
                AccountId = txn.AccountId,
                Amount = txn.Amount,
                Type = TransactionType.Deposit,
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

            _logger.LogInformation($"Deposit processed: {txn.Amount:C} to account {txn.AccountId}");

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
