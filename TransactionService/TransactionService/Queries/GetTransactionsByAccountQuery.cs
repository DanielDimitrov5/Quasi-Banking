using MediatR;
using TransactionService.Models;

namespace TransactionService.Queries;

// Query to get transactions by account ID
public record GetTransactionsByAccountQuery(string AccountId) : IRequest<List<TransactionResponse>>;
