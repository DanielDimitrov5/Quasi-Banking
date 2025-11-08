namespace AccountService.Endpoints;

public static class HealthEndpoints
{
    public static void MapHealthEndpoints(this WebApplication app)
    {
        app.MapGet("/health", () => Results.Ok(new
        {
            status = "healthy",
            service = "Account Service",
            timestamp = DateTime.UtcNow
        }))
        .WithName("HealthCheck")
        .WithOpenApi();
    }
}