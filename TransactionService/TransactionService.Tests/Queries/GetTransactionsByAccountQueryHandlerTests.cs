using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TransactionService.Data;
using TransactionService.Models;
using TransactionService.Queries;
using TransactionService.Tests.Helpers;
using Xunit;

namespace TransactionService.Tests.Queries;

public class GetTransactionsByAccountQueryHandlerTests : IDisposable
{
    private readonly TransactionDbContext _dbContext;

    public GetTransactionsByAccountQueryHandlerTests()
    {
        _dbContext = TestDbContextFactory.CreateTransactionDbContext();
    }

    [Fact]
    public async Task Handle_ExistingTransactions_ReturnsAllForAccount()
    {
        // Arrange
        var transactions = new[]
        {
            new Transaction
            {
                Id = "tx-1",
                AccountId = "account-123",
                Amount = 500m,
                Type = TransactionType.Deposit,
                Description = "Deposit 1",
                Status = TransactionStatus.Completed,
                CreatedAt = DateTime.UtcNow.AddDays(-2)
            },
            new Transaction
            {
                Id = "tx-2",
                AccountId = "account-123",
                Amount = -100m,
                Type = TransactionType.Withdrawal,
                Description = "Withdrawal 1",
                Status = TransactionStatus.Completed,
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            },
            new Transaction
            {
                Id = "tx-3",
                AccountId = "other-account",
                Amount = 200m,
                Type = TransactionType.Deposit,
                Description = "Other",
                Status = TransactionStatus.Completed,
                CreatedAt = DateTime.UtcNow
            }
        };

        await _dbContext.Transactions.AddRangeAsync(transactions);
        await _dbContext.SaveChangesAsync();

        var query = new GetTransactionsByAccountQuery("account-123");
        var handler = new GetTransactionsByAccountQueryHandler(_dbContext);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(t => t.AccountId == "account-123");
        result.Should().BeInDescendingOrder(t => t.CreatedAt);
    }

    [Fact]
    public async Task Handle_NoTransactions_ReturnsEmptyList()
    {
        // Arrange
        var query = new GetTransactionsByAccountQuery("non-existent");
        var handler = new GetTransactionsByAccountQueryHandler(_dbContext);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_TransactionsOrdered_ReturnsMostRecentFirst()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var transactions = new[]
        {
            new Transaction
            {
                Id = "tx-1",
                AccountId = "account-123",
                Amount = 100m,
                Type = TransactionType.Deposit,
                Description = "First",
                Status = TransactionStatus.Completed,
                CreatedAt = now.AddDays(-3)
            },
            new Transaction
            {
                Id = "tx-2",
                AccountId = "account-123",
                Amount = 200m,
                Type = TransactionType.Deposit,
                Description = "Second",
                Status = TransactionStatus.Completed,
                CreatedAt = now.AddDays(-2)
            },
            new Transaction
            {
                Id = "tx-3",
                AccountId = "account-123",
                Amount = 300m,
                Type = TransactionType.Deposit,
                Description = "Third",
                Status = TransactionStatus.Completed,
                CreatedAt = now.AddDays(-1)
            }
        };

        await _dbContext.Transactions.AddRangeAsync(transactions);
        await _dbContext.SaveChangesAsync();

        var query = new GetTransactionsByAccountQuery("account-123");
        var handler = new GetTransactionsByAccountQueryHandler(_dbContext);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(3);
        result[0].Description.Should().Be("Third");
        result[1].Description.Should().Be("Second");
        result[2].Description.Should().Be("First");
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}
