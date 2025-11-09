using ApiGateway.Middleware;
using ApiGateway.Endpoints;
using Prometheus;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });

    options.AddPolicy("AllowPrometheus", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("ApiGateway"))
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddJaegerExporter(options =>
            {
                options.AgentHost = "localhost"; // docker compose service name (or "localhost" if running locally)
                options.AgentPort = 6831;
            });
    });


// Add YARP Reverse Proxy
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

// Prometheus metrics
app.UseMetricServer();
app.UseHttpMetrics();

// Configure middleware pipeline
app.UseSwagger();
app.UseSwaggerUI();

app.UseWhen(
    ctx => ctx.Request.Path.StartsWithSegments("/metrics"),
    branch => branch.UseCors("AllowPrometheus")
);

app.UseCors("AllowFrontend");

// Add custom middleware
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<RateLimitingMiddleware>();

// Map health endpoints
app.MapHealthEndpoints();

// Map reverse proxy (this should be last)
app.MapReverseProxy();

Console.WriteLine("API Gateway starting on http://localhost:5050");
app.Run();
