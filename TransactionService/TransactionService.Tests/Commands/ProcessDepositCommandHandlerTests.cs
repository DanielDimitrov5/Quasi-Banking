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

public class ProcessDepositCommandHandlerTests : IDisposable
{
    private readonly TransactionDbContext _dbContext;
    private readonly Mock<IEventStore> _eventStoreMock;
    private readonly Mock<IAccountServiceClient> _accountServiceClientMock;
    private readonly Mock<ILogger<ProcessDepositCommandHandler>> _loggerMock;

    public ProcessDepositCommandHandlerTests()
    {
        _dbContext = TestDbContextFactory.CreateTransactionDbContext();
        _eventStoreMock = new Mock<IEventStore>();
        _accountServiceClientMock = new Mock<IAccountServiceClient>();
        _loggerMock = new Mock<ILogger<ProcessDepositCommandHandler>>();
    }

    [Fact]
    public async Task Handle_ValidDeposit_CreatesTransaction()
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

        var command = new ProcessDepositCommand(
            AccountId: "account-123",
            Amount: 500m,
            Description: "Salary"
        );

        var handler = new ProcessDepositCommandHandler(
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
        result.Amount.Should().Be(500m);
        result.Type.Should().Be(TransactionType.Deposit);
        result.Description.Should().Be("Salary");
        result.Status.Should().Be(TransactionStatus.Completed);

        // Verify transaction was saved
        var savedTransaction = await _dbContext.Transactions.FindAsync(result.Id);
        savedTransaction.Should().NotBeNull();
        savedTransaction!.Amount.Should().Be(500m);
    }

    [Fact]
    public async Task Handle_ValidDeposit_SavesEventToEventStore()
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

        var command = new ProcessDepositCommand(
            AccountId: "account-123",
            Amount: 500m,
            Description: "Salary"
        );

        var handler = new ProcessDepositCommandHandler(
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
                    e.Amount == 500m &&
                    e.Type == TransactionType.Deposit &&
                    e.Description == "Salary"
                ),
                It.IsAny<string>()
            ),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_ValidDeposit_CreatesOutboxMessage()
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

        var command = new ProcessDepositCommand(
            AccountId: "account-123",
            Amount: 500m,
            Description: "Salary"
        );

        var handler = new ProcessDepositCommandHandler(
            _dbContext,
            _eventStoreMock.Object,
            _accountServiceClientMock.Object,
            _loggerMock.Object
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        var outboxMessage = _dbContext.OutboxMessages
            .FirstOrDefault(o => o.AggregateId == result.AccountId);

        outboxMessage.Should().NotBeNull();
        outboxMessage!.EventType.Should().Be("TransactionProcessedEvent");
        outboxMessage.Status.Should().Be(OutboxStatus.Pending);
    }

    [Fact]
    public async Task Handle_NegativeAmount_ThrowsValidationException()
    {
        // Arrange
        var command = new ProcessDepositCommand(
            AccountId: "account-123",
            Amount: -100m,
            Description: "Invalid"
        );

        var handler = new ProcessDepositCommandHandler(
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
    public async Task Handle_ZeroAmount_ThrowsValidationException()
    {
        // Arrange
        var command = new ProcessDepositCommand(
            AccountId: "account-123",
            Amount: 0m,
            Description: "Zero"
        );

        var handler = new ProcessDepositCommandHandler(
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
    public async Task Handle_AmountExceedsLimit_ThrowsValidationException()
    {
        // Arrange
        var command = new ProcessDepositCommand(
            AccountId: "account-123",
            Amount: 2000000m,
            Description: "Too large"
        );

        var handler = new ProcessDepositCommandHandler(
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

        var command = new ProcessDepositCommand(
            AccountId: "non-existent",
            Amount: 100m,
            Description: "Test"
        );

        var handler = new ProcessDepositCommandHandler(
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

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}
