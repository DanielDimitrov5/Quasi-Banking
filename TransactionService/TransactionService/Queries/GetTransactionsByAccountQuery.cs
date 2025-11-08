using MediatR;
using TransactionService.Models;

namespace TransactionService.Queries;

public record GetTransactionsByAccountQuery(string AccountId) : IRequest<List<TransactionResponse>>;
