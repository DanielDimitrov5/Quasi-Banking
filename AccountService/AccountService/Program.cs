using AccountService.Data;
using AccountService.Services;
using AccountService.Endpoints;
using Microsoft.EntityFrameworkCore;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// SQL Server Database contexts
builder.Services.AddDbContext<AccountDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null
        )
    ));

builder.Services.AddDbContext<EventStoreContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure()
    ));

// Services
builder.Services.AddSingleton<IKafkaProducerService, KafkaProducerService>();
builder.Services.AddScoped<IEventStore, EventStore>();
builder.Services.AddHostedService<OutboxProcessor>();
builder.Services.AddHostedService<KafkaConsumerService>();

// MediatR for CQRS
builder.Services.AddMediatR(cfg => 
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

var app = builder.Build();

// Prometheus metrics
app.UseMetricServer();
app.UseHttpMetrics();

// Auto-migrate database on startup
using (var scope = app.Services.CreateScope())
{
    try
    {
        var accountDb = scope.ServiceProvider.GetRequiredService<AccountDbContext>();
        var eventDb = scope.ServiceProvider.GetRequiredService<EventStoreContext>();

        // Only run migrations if NOT using in-memory database
        if (accountDb.Database.ProviderName != "Microsoft.EntityFrameworkCore.InMemory")
        {
            accountDb.Database.Migrate();
        }
        else
        {
            accountDb.Database.EnsureCreated();
        }

        if (eventDb.Database.ProviderName != "Microsoft.EntityFrameworkCore.InMemory")
        {
            eventDb.Database.Migrate();
        }
        else
        {
            eventDb.Database.EnsureCreated();
        }

        Console.WriteLine("Account Service: Database migrations applied");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error applying migrations: {ex.Message}");
    }
}

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("AllowAll");

// Map endpoints
app.MapAccountEndpoints();
app.MapHealthEndpoints();

app.Run();

public partial class Program { }
