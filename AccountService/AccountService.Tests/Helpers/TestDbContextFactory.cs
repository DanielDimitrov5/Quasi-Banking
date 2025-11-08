using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using AccountService.Data;

namespace AccountService.Tests.Helpers;

public static class TestDbContextFactory
{
    public static AccountDbContext CreateAccountDbContext()
    {
        var options = new DbContextOptionsBuilder<AccountDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var context = new AccountDbContext(options);
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
