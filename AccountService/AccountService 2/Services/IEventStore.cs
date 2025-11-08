using AccountService.Models;

namespace AccountService.Services;

public interface IEventStore
{
    Task SaveEventAsync<T>(T @event, string aggregateId) where T : IEvent;
    Task<List<IEvent>> GetEventsAsync(string aggregateId);
}
