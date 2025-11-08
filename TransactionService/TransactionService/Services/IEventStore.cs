using TransactionService.Models;

namespace TransactionService.Services;

public interface IEventStore
{
    Task SaveEventAsync<T>(T @event, string aggregateId) where T : IEvent;
}
