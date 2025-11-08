namespace TransactionService.Services;

public interface IAccountServiceClient
{
    Task<AccountDto?> GetAccountAsync(string accountId);
}

public class AccountDto
{
    public string Id { get; set; } = null!;
    public string OwnerId { get; set; } = null!;
    public string OwnerEmail { get; set; } = null!;
    public decimal Balance { get; set; }
    public DateTime CreatedAt { get; set; }
}
