using AccountService.Data;
using AccountService.Models;
using AccountService.Services;
using AccountService.Tests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace AccountService.Tests.Services;

public class EventStoreTests : IDisposable
{
    private readonly EventStoreContext _eventStoreContext;
    private readonly EventStore _eventStore;

    public EventStoreTests()
    {
        _eventStoreContext = TestDbContextFactory.CreateEventStoreContext();
        _eventStore = new EventStore(_eventStoreContext);
    }

    [Fact]
    public async Task SaveEventAsync_ValidEvent_SavesToDatabase()
    {
        // Arrange
        var accountCreatedEvent = new AccountCreatedEvent
        {
            EventId = Guid.NewGuid().ToString(),
            AccountId = "account-123",
            OwnerId = "user-123",
            OwnerEmail = "test@example.com",
            InitialDeposit = 1000m
        };

        // Act
        await _eventStore.SaveEventAsync(accountCreatedEvent, "account-123");

        // Assert
        var savedEvents = await _eventStoreContext.Events
            .Where(e => e.AggregateId == "account-123")
            .ToListAsync();

        savedEvents.Should().HaveCount(1);
        savedEvents[0].EventType.Should().Be("AccountCreatedEvent");
        savedEvents[0].AggregateId.Should().Be("account-123");
    }

    [Fact]
    public async Task GetEventsAsync_MultipleEvents_ReturnsInOrder()
    {
        // Arrange
        var event1 = new AccountCreatedEvent
        {
            EventId = Guid.NewGuid().ToString(),
            AccountId = "account-123",
            OwnerId = "user-123",
            OwnerEmail = "test@example.com",
            InitialDeposit = 1000m,
            Timestamp = DateTime.UtcNow
        };

        await _eventStore.SaveEventAsync(event1, "account-123");
        
        await Task.Delay(100); // Ensure different timestamps

        var event2 = new BalanceUpdatedEvent
        {
            EventId = Guid.NewGuid().ToString(),
            AccountId = "account-123",
            Amount = 500m,
            NewBalance = 1500m,
            Timestamp = DateTime.UtcNow
        };

        await _eventStore.SaveEventAsync(event2, "account-123");

        // Act
        var events = await _eventStore.GetEventsAsync("account-123");

        // Assert
        events.Should().HaveCount(2);
        events[0].Should().BeOfType<AccountCreatedEvent>();
        events[1].Should().BeOfType<BalanceUpdatedEvent>();
    }

    public void Dispose()
    {
        _eventStoreContext.Dispose();
    }
}
