namespace TransactionService.Models;

public class OutboxMessage
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string EventType { get; set; } = null!;
    public string AggregateId { get; set; } = null!;
    public string Payload { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
    public OutboxStatus Status { get; set; } = OutboxStatus.Pending;
    public int RetryCount { get; set; } = 0;
    public string? Error { get; set; }
}

public enum OutboxStatus
{
    Pending,
    Processing,
    Completed,
    Failed
}
