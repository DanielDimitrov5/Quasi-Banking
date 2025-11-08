using MediatR;
using TransactionService.Models;

namespace TransactionService.Commands;

public record ProcessDepositCommand(
    string AccountId,
    decimal Amount,
    string Description
) : IRequest<TransactionResponse>;
