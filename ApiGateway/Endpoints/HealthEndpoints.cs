namespace ApiGateway.Endpoints;

public static class HealthEndpoints
{
    public static void MapHealthEndpoints(this WebApplication app)
    {
        app.MapGet("/health", () => Results.Ok(new
        {
            status = "healthy",
            service = "API Gateway",
            timestamp = DateTime.UtcNow
        }))
        .WithName("HealthCheck")
        .WithOpenApi();

        app.MapGet("/health/services", async (IHttpClientFactory httpClientFactory) =>
        {
            var services = new Dictionary<string, object>();

            // Check Account Service
            try
            {
                var accountClient = httpClientFactory.CreateClient();
                var accountResponse = await accountClient.GetAsync("http://localhost:5199/health");
                services["AccountService"] = new
                {
                    status = accountResponse.IsSuccessStatusCode ? "healthy" : "unhealthy",
                    statusCode = (int)accountResponse.StatusCode
                };
            }
            catch
            {
                services["AccountService"] = new { status = "unreachable" };
            }

            // Check Transaction Service
            try
            {
                var transactionClient = httpClientFactory.CreateClient();
                var transactionResponse = await transactionClient.GetAsync("http://localhost:5002/health");
                services["TransactionService"] = new
                {
                    status = transactionResponse.IsSuccessStatusCode ? "healthy" : "unhealthy",
                    statusCode = (int)transactionResponse.StatusCode
                };
            }
            catch
            {
                services["TransactionService"] = new { status = "unreachable" };
            }

            return Results.Ok(new
            {
                gateway = "healthy",
                services,
                timestamp = DateTime.UtcNow
            });
        })
        .WithName("ServiceHealth")
        .WithOpenApi();
    }
}
