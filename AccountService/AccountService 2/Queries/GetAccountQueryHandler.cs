using AccountService.Data;
using AccountService.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AccountService.Queries;

public class GetAccountQueryHandler : IRequestHandler<GetAccountQuery, AccountResponse?>
{
    private readonly AccountDbContext _dbContext;

    public GetAccountQueryHandler(AccountDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AccountResponse?> Handle(GetAccountQuery request, CancellationToken cancellationToken)
    {
        var account = await _dbContext.Accounts
            .FirstOrDefaultAsync(a => a.Id == request.AccountId, cancellationToken);

        if (account == null) return null;

        return new AccountResponse(
            account.Id,
            account.OwnerId,
            account.OwnerEmail,
            account.Balance,
            account.CreatedAt
        );
    }
}
