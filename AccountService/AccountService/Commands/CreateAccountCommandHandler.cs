using System.Text.Json;
using AccountService.Data;
using AccountService.Models;
using AccountService.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace AccountService.Commands;

public class CreateAccountCommandHandler : IRequestHandler<CreateAccountCommand, AccountResponse>
{
    private readonly AccountDbContext _dbContext;
    private readonly IEventStore _eventStore;

    public CreateAccountCommandHandler(
        AccountDbContext dbContext,
        IEventStore eventStore)
    {
        _dbContext = dbContext;
        _eventStore = eventStore;
    }

    // Handle the command to create a new account
    public async Task<AccountResponse> Handle(CreateAccountCommand request, CancellationToken cancellationToken)
    {
        // Create an execution strategy for handling retries
        IExecutionStrategy strategy = _dbContext.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            // Begin a new transaction
            using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var account = new Account
                {
                    Id = Guid.NewGuid().ToString(),
                    OwnerId = request.OwnerId,
                    OwnerEmail = request.OwnerEmail,
                    Balance = request.InitialDeposit,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var @event = new AccountCreatedEvent
                {
                    AccountId = account.Id,
                    OwnerId = account.OwnerId,
                    OwnerEmail = account.OwnerEmail,
                    InitialDeposit = request.InitialDeposit
                };

                // Save the event to the event store
                await _eventStore.SaveEventAsync(@event, account.Id);

                _dbContext.Accounts.Add(account);

                var outboxMessage = new OutboxMessage
                {
                    EventType = @event.EventType,
                    AggregateId = account.Id,
                    Payload = JsonSerializer.Serialize(@event),
                    Status = OutboxStatus.Pending
                };
                _dbContext.OutboxMessages.Add(outboxMessage);

                // Save changes to the database, commit the transaction
                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return new AccountResponse(
                    account.Id,
                    account.OwnerId,
                    account.OwnerEmail,
                    account.Balance,
                    account.CreatedAt
                );
            }
            catch
            {
                // Rollback the transaction in case of an error
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }
}
