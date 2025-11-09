using AccountService.Commands;
using AccountService.Data;
using AccountService.Models;
using AccountService.Services;
using AccountService.Tests.Helpers;
using FluentAssertions;
using Moq;

namespace AccountService.Tests.Commands;

public class CreateAccountCommandHandlerTests : IDisposable
{
    private readonly AccountDbContext _accountDbContext;
    private readonly EventStoreContext _eventStoreContext;
    private readonly Mock<IEventStore> _eventStoreMock;

    public CreateAccountCommandHandlerTests()
    {
        _accountDbContext = TestDbContextFactory.CreateAccountDbContext();
        _eventStoreContext = TestDbContextFactory.CreateEventStoreContext();
        _eventStoreMock = new Mock<IEventStore>();
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesAccount()
    {
        // Arrange
        var command = new CreateAccountCommand(
            OwnerId: "user-123",
            OwnerEmail: "test@example.com",
            InitialDeposit: 1000m
        );

        var handler = new CreateAccountCommandHandler(
            _accountDbContext,
            _eventStoreMock.Object
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.OwnerId.Should().Be("user-123");
        result.OwnerEmail.Should().Be("test@example.com");
        result.Balance.Should().Be(1000m);

        // Verify account was saved to database
        var savedAccount = await _accountDbContext.Accounts.FindAsync(result.Id);
        savedAccount.Should().NotBeNull();
        savedAccount!.Balance.Should().Be(1000m);
    }

    [Fact]
    public async Task Handle_ValidCommand_SavesEventToEventStore()
    {
        // Arrange
        var command = new CreateAccountCommand(
            OwnerId: "user-123",
            OwnerEmail: "test@example.com",
            InitialDeposit: 1000m
        );

        var handler = new CreateAccountCommandHandler(
            _accountDbContext,
            _eventStoreMock.Object
        );

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        _eventStoreMock.Verify(
            x => x.SaveEventAsync(
                It.Is<AccountCreatedEvent>(e =>
                    e.OwnerId == "user-123" &&
                    e.OwnerEmail == "test@example.com" &&
                    e.InitialDeposit == 1000m
                ),
                It.IsAny<string>()
            ),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesOutboxMessage()
    {
        // Arrange
        var command = new CreateAccountCommand(
            OwnerId: "user-123",
            OwnerEmail: "test@example.com",
            InitialDeposit: 1000m
        );

        var handler = new CreateAccountCommandHandler(
            _accountDbContext,
            _eventStoreMock.Object
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        var outboxMessage = _accountDbContext.OutboxMessages
            .FirstOrDefault(o => o.AggregateId == result.Id);

        outboxMessage.Should().NotBeNull();
        outboxMessage!.EventType.Should().Be("AccountCreatedEvent");
        outboxMessage.Status.Should().Be(OutboxStatus.Pending);
    }

    [Theory]
    [InlineData(-100)]
    [InlineData(0)]
    public async Task Handle_NegativeOrZeroDeposit_ShouldStillCreate(decimal initialDeposit)
    {
        // Arrange - Testing edge cases
        var command = new CreateAccountCommand(
            OwnerId: "user-123",
            OwnerEmail: "test@example.com",
            InitialDeposit: initialDeposit
        );

        var handler = new CreateAccountCommandHandler(
            _accountDbContext,
            _eventStoreMock.Object
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Balance.Should().Be(initialDeposit);
    }

    public void Dispose()
    {
        _accountDbContext.Dispose();
        _eventStoreContext.Dispose();
    }
}
