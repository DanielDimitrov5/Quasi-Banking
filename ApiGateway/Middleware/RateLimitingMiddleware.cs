using System.Collections.Concurrent;

namespace ApiGateway.Middleware;

public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private static readonly ConcurrentDictionary<string, (DateTime FirstRequest, int Count)> _requestCounts = new();
    private readonly int _maxRequests = 100; // Max requests per window
    private readonly TimeSpan _timeWindow = TimeSpan.FromMinutes(1);

    public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var now = DateTime.UtcNow;

        // Clean up old entries
        CleanupOldEntries(now);

        if (_requestCounts.TryGetValue(clientIp, out var info))
        {
            if (now - info.FirstRequest < _timeWindow)
            {
                if (info.Count >= _maxRequests)
                {
                    _logger.LogWarning($"⛔ Rate limit exceeded for {clientIp}");
                    context.Response.StatusCode = 429; // Too Many Requests
                    await context.Response.WriteAsJsonAsync(new
                    {
                        error = "Rate limit exceeded. Please try again later.",
                        retryAfter = _timeWindow.TotalSeconds
                    });
                    return;
                }

                _requestCounts[clientIp] = (info.FirstRequest, info.Count + 1);
            }
            else
            {
                _requestCounts[clientIp] = (now, 1);
            }
        }
        else
        {
            _requestCounts[clientIp] = (now, 1);
        }

        await _next(context);
    }

    private void CleanupOldEntries(DateTime now)
    {
        var keysToRemove = _requestCounts
            .Where(kvp => now - kvp.Value.FirstRequest > _timeWindow)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in keysToRemove)
        {
            _requestCounts.TryRemove(key, out _);
        }
    }
}
