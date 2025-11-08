using Microsoft.EntityFrameworkCore;
using TransactionService.Data;
using TransactionService.Endpoints;
using TransactionService.Services;
using TransactionService.Middlewares;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

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

// Database contexts
builder.Services.AddDbContext<TransactionDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddDbContext<EventStoreContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));


// HTTP Client for Account Service
builder.Services.AddHttpClient<IAccountServiceClient, AccountServiceClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:AccountService"] ?? "http://localhost:5199");
    client.Timeout = TimeSpan.FromSeconds(10);
});

// Services
builder.Services.AddSingleton<KafkaProducerService>();
builder.Services.AddScoped<IEventStore, EventStore>();
builder.Services.AddHostedService<OutboxProcessor>();

// MediatR
builder.Services.AddMediatR(cfg => 
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

var app = builder.Build();

// Prometheus metrics
app.UseMetricServer();
app.UseHttpMetrics();

// Auto-migrate
using (var scope = app.Services.CreateScope())
{
    try
    {
        var transactionDb = scope.ServiceProvider.GetRequiredService<TransactionDbContext>();
        var eventDb = scope.ServiceProvider.GetRequiredService<EventStoreContext>();

        transactionDb.Database.Migrate();
        eventDb.Database.Migrate();

        Console.WriteLine("✅ Transaction Service: Database migrations applied");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Error applying migrations: {ex.Message}");
    }
}

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("AllowAll");

// Add exception handler middleware
app.UseMiddleware<GlobalExceptionHandler>();

app.MapTransactionEndpoints();

app.Run();
