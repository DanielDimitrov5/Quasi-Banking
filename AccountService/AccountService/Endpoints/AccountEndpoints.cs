using AccountService.Commands;
using AccountService.Models;
using AccountService.Queries;
using MediatR;

namespace AccountService.Endpoints;

public static class AccountEndpoints
{
    public static void MapAccountEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/accounts").WithTags("Accounts");

        group.MapPost("/", async (CreateAccountRequest request, IMediator mediator) =>
        {
            var command = new CreateAccountCommand(
                request.OwnerId,
                request.OwnerEmail,
                request.InitialDeposit
            );

            AccountResponse account = await mediator.Send(command);
            return Results.Created($"/api/accounts/{account.Id}", account);
        })
        .WithName("CreateAccount")
        .WithOpenApi();

        group.MapGet("/{id}", async (string id, IMediator mediator) =>
        {
            GetAccountQuery query = new GetAccountQuery(id);
            AccountResponse? account = await mediator.Send(query);

            return account is not null ? Results.Ok(account) : Results.NotFound();
        })
        .WithName("GetAccount")
        .WithOpenApi();
    }
}
