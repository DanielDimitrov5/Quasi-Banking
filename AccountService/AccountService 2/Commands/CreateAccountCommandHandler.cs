using System.Text.Json;
using AccountService.Data;
using AccountService.Models;
using AccountService.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

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

    public async Task<AccountResponse> Handle(CreateAccountCommand request, CancellationToken cancellationToken)
    {
        var strategy = _dbContext.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
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
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }
}
