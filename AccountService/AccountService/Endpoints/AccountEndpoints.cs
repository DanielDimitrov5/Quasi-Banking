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

            var account = await mediator.Send(command);
            return Results.Created($"/api/accounts/{account.Id}", account);
        })
        .WithName("CreateAccount")
        .WithOpenApi();

        group.MapGet("/{id}", async (string id, IMediator mediator) =>
        {
            var query = new GetAccountQuery(id);
            var account = await mediator.Send(query);

            return account is not null ? Results.Ok(account) : Results.NotFound();
        })
        .WithName("GetAccount")
        .WithOpenApi();
    }
}
