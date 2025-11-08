namespace TransactionService.Models;

public interface IEvent
{
    string EventId { get; }
    string EventType { get; }
    DateTime Timestamp { get; }
}

public class TransactionProcessedEvent : IEvent
{
    public string EventId { get; set; } = Guid.NewGuid().ToString();
    public string EventType { get; set; } = nameof(TransactionProcessedEvent);
    public string TransactionId { get; set; } = null!;
    public string AccountId { get; set; } = null!;
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }
    public string Description { get; set; } = null!;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class BalanceUpdatedEvent : IEvent
{
    public string EventId { get; set; } = Guid.NewGuid().ToString();
    public string EventType { get; set; } = nameof(BalanceUpdatedEvent);
    public string AccountId { get; set; } = null!;
    public decimal Amount { get; set; }
    public decimal NewBalance { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
