using MediatR;
using TransactionService.Models;

namespace TransactionService.Commands;

// Command to process a deposit transaction
public record ProcessDepositCommand(
    string AccountId,
    decimal Amount,
    string Description
) : IRequest<TransactionResponse>;
