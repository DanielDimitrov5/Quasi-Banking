namespace AccountService.Models;

public interface IEvent
{
    string EventId { get; }
    string EventType { get; }
    DateTime Timestamp { get; }
}

public class AccountCreatedEvent : IEvent
{
    public string EventId { get; set; } = Guid.NewGuid().ToString();
    public string EventType { get; set; } = nameof(AccountCreatedEvent);
    public string AccountId { get; set; } = null!;
    public string OwnerId { get; set; } = null!;
    public string OwnerEmail { get; set; } = null!;
    public decimal InitialDeposit { get; set; }
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
