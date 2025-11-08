using MediatR;
using Microsoft.EntityFrameworkCore;
using TransactionService.Data;
using TransactionService.Models;

namespace TransactionService.Queries;

public class GetTransactionsByAccountQueryHandler 
    : IRequestHandler<GetTransactionsByAccountQuery, List<TransactionResponse>>
{
    private readonly TransactionDbContext _dbContext;

    public GetTransactionsByAccountQueryHandler(TransactionDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<TransactionResponse>> Handle(
        GetTransactionsByAccountQuery request, 
        CancellationToken cancellationToken)
    {
        var transactions = await _dbContext.Transactions
            .Where(t => t.AccountId == request.AccountId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);

        return transactions.Select(t => new TransactionResponse(
            t.Id,
            t.AccountId,
            t.Amount,
            t.Type,
            t.Description,
            t.Status,
            t.CreatedAt
        )).ToList();
    }
}
