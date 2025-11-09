using AccountService.Models;
using MediatR;

namespace AccountService.Queries;

// Query to get account details by account ID
public record GetAccountQuery(string AccountId) : IRequest<AccountResponse?>;
