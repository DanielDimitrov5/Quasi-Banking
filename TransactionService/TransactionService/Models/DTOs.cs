namespace TransactionService.Models;

public record ProcessTransactionRequest(
    string AccountId,
    decimal Amount,
    string Description
);

public record TransactionResponse(
    string Id,
    string AccountId,
    decimal Amount,
    TransactionType Type,
    string Description,
    TransactionStatus Status,
    DateTime CreatedAt
);
