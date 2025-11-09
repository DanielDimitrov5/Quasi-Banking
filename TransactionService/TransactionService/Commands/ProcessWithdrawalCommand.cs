using MediatR;
using TransactionService.Models;

namespace TransactionService.Commands;

// Command to process a withdrawal transaction
public record ProcessWithdrawalCommand(
    string AccountId,
    decimal Amount,
    string Description
) : IRequest<TransactionResponse>;
