namespace TransactionService.Models;

public class ValidationException : Exception
{
    public string ErrorCode { get; }

    public ValidationException(string message, string errorCode = "VALIDATION_ERROR") 
        : base(message)
    {
        ErrorCode = errorCode;
    }
}

public class InsufficientFundsException : ValidationException
{
    public decimal Available { get; }
    public decimal Requested { get; }

    public InsufficientFundsException(decimal available, decimal requested)
        : base($"Insufficient funds. Available: {available:C}, Requested: {requested:C}", "INSUFFICIENT_FUNDS")
    {
        Available = available;
        Requested = requested;
    }
}

public class AccountNotFoundException : ValidationException
{
    public string AccountId { get; }

    public AccountNotFoundException(string accountId)
        : base($"Account {accountId} not found", "ACCOUNT_NOT_FOUND")
    {
        AccountId = accountId;
    }
}
