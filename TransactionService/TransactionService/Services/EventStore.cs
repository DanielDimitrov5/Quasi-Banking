using System.Text.Json;
using TransactionService.Data;
using TransactionService.Models;

namespace TransactionService.Services;

public class EventStore : IEventStore
{
    private readonly EventStoreContext _context;

    public EventStore(EventStoreContext context)
    {
        _context = context;
    }

    public async Task SaveEventAsync<T>(T @event, string aggregateId) where T : IEvent
    {
        var storedEvent = new StoredEvent
        {
            EventType = @event.EventType,
            AggregateId = aggregateId,
            EventData = JsonSerializer.Serialize(@event),
            Timestamp = @event.Timestamp
        };

        _context.Events.Add(storedEvent);
        await _context.SaveChangesAsync();
    }
}
