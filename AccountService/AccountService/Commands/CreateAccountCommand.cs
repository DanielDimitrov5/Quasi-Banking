using AccountService.Models;
using MediatR;

namespace AccountService.Commands;

public record CreateAccountCommand(
    string OwnerId,
    string OwnerEmail,
    decimal InitialDeposit
) : IRequest<AccountResponse>;
