using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using AccountService.Data;
using AccountService.Services;
using Moq;

namespace AccountService.IntegrationTests;

public class WebApplicationFactoryFixture : WebApplicationFactory<Program>
{
    public Mock<IKafkaProducerService> KafkaProducerMock { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the existing DbContext registrations
            services.RemoveAll(typeof(DbContextOptions<AccountDbContext>));
            services.RemoveAll(typeof(DbContextOptions<EventStoreContext>));

            // Add in-memory database for testing with transaction warning suppressed
            services.AddDbContext<AccountDbContext>(options =>
            {
                options.UseInMemoryDatabase("TestAccountDb")
                       .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning));
            });

            services.AddDbContext<EventStoreContext>(options =>
            {
                options.UseInMemoryDatabase("TestEventStoreDb")
                       .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning));
            });

            // Replace KafkaProducerService with mock
            services.RemoveAll<IKafkaProducerService>();
            services.AddSingleton(KafkaProducerMock.Object);

            // Build the service provider
            var sp = services.BuildServiceProvider();

            // Create a scope to obtain a reference to the database contexts
            using var scope = sp.CreateScope();
            var scopedServices = scope.ServiceProvider;
            var accountDb = scopedServices.GetRequiredService<AccountDbContext>();
            var eventDb = scopedServices.GetRequiredService<EventStoreContext>();

            // Ensure the databases are created
            accountDb.Database.EnsureCreated();
            eventDb.Database.EnsureCreated();
        });
    }
}
