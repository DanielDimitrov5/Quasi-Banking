using System.Net;
using System.Text.Json;
using TransactionService.Models;

namespace TransactionService.Middlewares;

public class GlobalExceptionHandler
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(RequestDelegate next, ILogger<GlobalExceptionHandler> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation error: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex, HttpStatusCode.BadRequest, ex.ErrorCode);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Service communication error");
            await HandleExceptionAsync(context, ex, HttpStatusCode.ServiceUnavailable, "SERVICE_UNAVAILABLE");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await HandleExceptionAsync(context, ex, HttpStatusCode.InternalServerError, "INTERNAL_ERROR");
        }
    }

    private static async Task HandleExceptionAsync(
        HttpContext context, 
        Exception exception, 
        HttpStatusCode statusCode,
        string errorCode = "ERROR")
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var response = new
        {
            error = exception.Message,
            errorCode,
            statusCode = (int)statusCode,
            timestamp = DateTime.UtcNow
        };

        var json = JsonSerializer.Serialize(response);
        await context.Response.WriteAsync(json);
    }
}
