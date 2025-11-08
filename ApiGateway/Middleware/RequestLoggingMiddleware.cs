namespace ApiGateway.Middleware;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var startTime = DateTime.UtcNow;
        var requestPath = context.Request.Path;
        var requestMethod = context.Request.Method;

        _logger.LogInformation($"🌐 Incoming: {requestMethod} {requestPath}");

        try
        {
            await _next(context);

            var duration = DateTime.UtcNow - startTime;
            var statusCode = context.Response.StatusCode;

            var emoji = statusCode switch
            {
                >= 200 and < 300 => "✅",
                >= 400 and < 500 => "⚠️",
                >= 500 => "❌",
                _ => "ℹ️"
            };

            _logger.LogInformation(
                $"{emoji} Completed: {requestMethod} {requestPath} → {statusCode} in {duration.TotalMilliseconds:F2}ms");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"❌ Error processing {requestMethod} {requestPath}");
            throw;
        }
    }
}
