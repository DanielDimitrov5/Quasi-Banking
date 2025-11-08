using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TransactionService.Data;
using TransactionService.Models;
using TransactionService.Services;
using TransactionService.Tests.Helpers;
using Xunit;

namespace TransactionService.Tests.Services;

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
        var transactionEvent = new TransactionProcessedEvent
        {
            EventId = Guid.NewGuid().ToString(),
            TransactionId = "tx-123",
            AccountId = "account-123",
            Amount = 500m,
            Type = TransactionType.Deposit,
            Description = "Test deposit"
        };

        // Act
        await _eventStore.SaveEventAsync(transactionEvent, "tx-123");

        // Assert
        var savedEvents = await _eventStoreContext.Events
            .Where(e => e.AggregateId == "tx-123")
            .ToListAsync();

        savedEvents.Should().HaveCount(1);
        savedEvents[0].EventType.Should().Be("TransactionProcessedEvent");
        savedEvents[0].AggregateId.Should().Be("tx-123");
    }

    public void Dispose()
    {
        _eventStoreContext.Dispose();
    }
}
