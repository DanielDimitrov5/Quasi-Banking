using AccountService.Data;
using AccountService.Models;
using AccountService.Queries;
using AccountService.Tests.Helpers;
using FluentAssertions;
using Xunit;

namespace AccountService.Tests.Queries;

public class GetAccountQueryHandlerTests : IDisposable
{
    private readonly AccountDbContext _accountDbContext;

    public GetAccountQueryHandlerTests()
    {
        _accountDbContext = TestDbContextFactory.CreateAccountDbContext();
    }

    [Fact]
    public async Task Handle_ExistingAccount_ReturnsAccount()
    {
        // Arrange
        var account = new Account
        {
            Id = "test-account-123",
            OwnerId = "user-123",
            OwnerEmail = "test@example.com",
            Balance = 1000m,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _accountDbContext.Accounts.Add(account);
        await _accountDbContext.SaveChangesAsync();

        var query = new GetAccountQuery("test-account-123");
        var handler = new GetAccountQueryHandler(_accountDbContext);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be("test-account-123");
        result.OwnerId.Should().Be("user-123");
        result.Balance.Should().Be(1000m);
    }

    [Fact]
    public async Task Handle_NonExistingAccount_ReturnsNull()
    {
        // Arrange
        var query = new GetAccountQuery("non-existing-id");
        var handler = new GetAccountQueryHandler(_accountDbContext);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    public void Dispose()
    {
        _accountDbContext.Dispose();
    }
}