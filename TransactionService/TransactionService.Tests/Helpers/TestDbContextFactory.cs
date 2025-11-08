using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TransactionService.Data;

namespace TransactionService.Tests.Helpers;

public static class TestDbContextFactory
{
    public static TransactionDbContext CreateTransactionDbContext()
    {
        var options = new DbContextOptionsBuilder<TransactionDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var context = new TransactionDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    public static EventStoreContext CreateEventStoreContext()
    {
        var options = new DbContextOptionsBuilder<EventStoreContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var context = new EventStoreContext(options);
        context.Database.EnsureCreated();
        return context;
    }
}
