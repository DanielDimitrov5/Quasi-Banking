using MediatR;
using TransactionService.Models;

namespace TransactionService.Commands;

public record ProcessWithdrawalCommand(
    string AccountId,
    decimal Amount,
    string Description
) : IRequest<TransactionResponse>;
