namespace AccountService.Models;

public class Account
{
    public string Id { get; set; } = null!;
    public string OwnerId { get; set; } = null!;
    public string OwnerEmail { get; set; } = null!;
    public decimal Balance { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
