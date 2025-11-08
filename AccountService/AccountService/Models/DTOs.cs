namespace AccountService.Models;

public record CreateAccountRequest(
    string OwnerId,
    string OwnerEmail,
    decimal InitialDeposit
);

public record AccountResponse(
    string Id, 
    string OwnerId, 
    string OwnerEmail, 
    decimal Balance, 
    DateTime CreatedAt
);
