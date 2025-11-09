using AccountService.Models;
using MediatR;

namespace AccountService.Commands;

// Command to create a new account
public record CreateAccountCommand(
    string OwnerId,
    string OwnerEmail,
    decimal InitialDeposit
) : IRequest<AccountResponse>;
