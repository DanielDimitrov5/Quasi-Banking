using System.Text.Json;
using AccountService.Data;
using AccountService.Models;
using Microsoft.EntityFrameworkCore;

namespace AccountService.Services;

// Implementation of the event store
// Used to save and retrieve events
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

    public async Task<List<IEvent>> GetEventsAsync(string aggregateId)
    {
        var storedEvents = await _context.Events
            .Where(e => e.AggregateId == aggregateId)
            .OrderBy(e => e.Timestamp)
            .ToListAsync();

        var events = new List<IEvent>();
        foreach (var stored in storedEvents)
        {
            var eventType = Type.GetType($"AccountService.Models.{stored.EventType}");
            if (eventType != null)
            {
                var @event = JsonSerializer.Deserialize(stored.EventData, eventType) as IEvent;
                if (@event != null) events.Add(@event);
            }
        }

        return events;
    }
}
