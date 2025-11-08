using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TransactionService.Commands;
using TransactionService.Data;
using TransactionService.Models;
using TransactionService.Services;
using TransactionService.Tests.Helpers;
using Xunit;

namespace TransactionService.Tests.Commands;

public class ProcessWithdrawalCommandHandlerTests : IDisposable
{
    private readonly TransactionDbContext _dbContext;
    private readonly Mock<IEventStore> _eventStoreMock;
    private readonly Mock<IAccountServiceClient> _accountServiceClientMock;
    private readonly Mock<ILogger<ProcessWithdrawalCommandHandler>> _loggerMock;

    public ProcessWithdrawalCommandHandlerTests()
    {
        _dbContext = TestDbContextFactory.CreateTransactionDbContext();
        _eventStoreMock = new Mock<IEventStore>();
        _accountServiceClientMock = new Mock<IAccountServiceClient>();
        _loggerMock = new Mock<ILogger<ProcessWithdrawalCommandHandler>>();
    }

    [Fact]
    public async Task Handle_ValidWithdrawal_CreatesTransaction()
    {
        // Arrange
        var accountDto = new AccountDto
        {
            Id = "account-123",
            OwnerId = "user-123",
            OwnerEmail = "test@example.com",
            Balance = 1000m
        };

        _accountServiceClientMock
            .Setup(x => x.GetAccountAsync("account-123"))
            .ReturnsAsync(accountDto);

        var command = new ProcessWithdrawalCommand(
            AccountId: "account-123",
            Amount: 200m,
            Description: "ATM Withdrawal"
        );

        var handler = new ProcessWithdrawalCommandHandler(
            _dbContext,
            _eventStoreMock.Object,
            _accountServiceClientMock.Object,
            _loggerMock.Object
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.AccountId.Should().Be("account-123");
        result.Amount.Should().Be(-200m); // Negative for withdrawal
        result.Type.Should().Be(TransactionType.Withdrawal);
        result.Description.Should().Be("ATM Withdrawal");
        result.Status.Should().Be(TransactionStatus.Completed);
    }

    [Fact]
    public async Task Handle_InsufficientFunds_ThrowsInsufficientFundsException()
    {
        // Arrange
        var accountDto = new AccountDto
        {
            Id = "account-123",
            OwnerId = "user-123",
            OwnerEmail = "test@example.com",
            Balance = 100m
        };

        _accountServiceClientMock
            .Setup(x => x.GetAccountAsync("account-123"))
            .ReturnsAsync(accountDto);

        var command = new ProcessWithdrawalCommand(
            AccountId: "account-123",
            Amount: 500m,
            Description: "Large withdrawal"
        );

        var handler = new ProcessWithdrawalCommandHandler(
            _dbContext,
            _eventStoreMock.Object,
            _accountServiceClientMock.Object,
            _loggerMock.Object
        );

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InsufficientFundsException>(
            () => handler.Handle(command, CancellationToken.None)
        );

        exception.Available.Should().Be(100m);
        exception.Requested.Should().Be(500m);
    }

    [Fact]
    public async Task Handle_AmountExceedsDailyLimit_ThrowsValidationException()
    {
        // Arrange
        var accountDto = new AccountDto
        {
            Id = "account-123",
            OwnerId = "user-123",
            OwnerEmail = "test@example.com",
            Balance = 50000m
        };

        _accountServiceClientMock
            .Setup(x => x.GetAccountAsync("account-123"))
            .ReturnsAsync(accountDto);

        var command = new ProcessWithdrawalCommand(
            AccountId: "account-123",
            Amount: 15000m,
            Description: "Exceeds limit"
        );

        var handler = new ProcessWithdrawalCommandHandler(
            _dbContext,
            _eventStoreMock.Object,
            _accountServiceClientMock.Object,
            _loggerMock.Object
        );

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => handler.Handle(command, CancellationToken.None)
        );
    }

    [Fact]
    public async Task Handle_NegativeAmount_ThrowsValidationException()
    {
        // Arrange
        var command = new ProcessWithdrawalCommand(
            AccountId: "account-123",
            Amount: -100m,
            Description: "Invalid"
        );

        var handler = new ProcessWithdrawalCommandHandler(
            _dbContext,
            _eventStoreMock.Object,
            _accountServiceClientMock.Object,
            _loggerMock.Object
        );

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => handler.Handle(command, CancellationToken.None)
        );
    }

    [Fact]
    public async Task Handle_NonExistentAccount_ThrowsAccountNotFoundException()
    {
        // Arrange
        _accountServiceClientMock
            .Setup(x => x.GetAccountAsync("non-existent"))
            .ReturnsAsync((AccountDto?)null);

        var command = new ProcessWithdrawalCommand(
            AccountId: "non-existent",
            Amount: 100m,
            Description: "Test"
        );

        var handler = new ProcessWithdrawalCommandHandler(
            _dbContext,
            _eventStoreMock.Object,
            _accountServiceClientMock.Object,
            _loggerMock.Object
        );

        // Act & Assert
        await Assert.ThrowsAsync<AccountNotFoundException>(
            () => handler.Handle(command, CancellationToken.None)
        );
    }

    [Fact]
    public async Task Handle_ValidWithdrawal_SavesEventToEventStore()
    {
        // Arrange
        var accountDto = new AccountDto
        {
            Id = "account-123",
            OwnerId = "user-123",
            OwnerEmail = "test@example.com",
            Balance = 1000m
        };

        _accountServiceClientMock
            .Setup(x => x.GetAccountAsync("account-123"))
            .ReturnsAsync(accountDto);

        var command = new ProcessWithdrawalCommand(
            AccountId: "account-123",
            Amount: 200m,
            Description: "ATM Withdrawal"
        );

        var handler = new ProcessWithdrawalCommandHandler(
            _dbContext,
            _eventStoreMock.Object,
            _accountServiceClientMock.Object,
            _loggerMock.Object
        );

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        _eventStoreMock.Verify(
            x => x.SaveEventAsync(
                It.Is<TransactionProcessedEvent>(e =>
                    e.AccountId == "account-123" &&
                    e.Amount == -200m &&
                    e.Type == TransactionType.Withdrawal
                ),
                It.IsAny<string>()
            ),
            Times.Once
        );
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}
