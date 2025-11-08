using MediatR;
using TransactionService.Commands;
using TransactionService.Models;
using TransactionService.Queries;

namespace TransactionService.Endpoints;

public static class TransactionEndpoints
{
    public static void MapTransactionEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/transactions").WithTags("Transactions");

        group.MapPost("/deposit", async (ProcessTransactionRequest request, IMediator mediator) =>
        {
            var command = new ProcessDepositCommand(
                request.AccountId,
                request.Amount,
                request.Description
            );

            var result = await mediator.Send(command);
            return Results.Ok(result);
        })
        .WithName("Deposit")
        .WithOpenApi();

        group.MapPost("/withdraw", async (ProcessTransactionRequest request, IMediator mediator) =>
        {
            var command = new ProcessWithdrawalCommand(
                request.AccountId,
                request.Amount,
                request.Description
            );

            var result = await mediator.Send(command);
            return Results.Ok(result);
        })
        .WithName("Withdraw")
        .WithOpenApi();

        group.MapGet("/account/{accountId}", async (string accountId, IMediator mediator) =>
        {
            var query = new GetTransactionsByAccountQuery(accountId);
            var transactions = await mediator.Send(query);
            return Results.Ok(transactions);
        })
        .WithName("GetTransactionsByAccount")
        .WithOpenApi();
    }
}
