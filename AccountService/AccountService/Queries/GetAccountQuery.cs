using AccountService.Models;
using MediatR;

namespace AccountService.Queries;

public record GetAccountQuery(string AccountId) : IRequest<AccountResponse?>;
