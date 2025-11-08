namespace TransactionService.Models;

public class Transaction
{
    public string Id { get; set; } = null!;
    public string AccountId { get; set; } = null!;
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }
    public string Description { get; set; } = null!;
    public TransactionStatus Status { get; set; } = TransactionStatus.Completed;
    public DateTime CreatedAt { get; set; }
}

public enum TransactionType
{
    Deposit,
    Withdrawal
}

public enum TransactionStatus
{
    Pending,
    Completed,
    Failed
}
