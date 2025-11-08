using Microsoft.EntityFrameworkCore;

namespace TransactionService.Data;

public class EventStoreContext : DbContext
{
    public EventStoreContext(DbContextOptions<EventStoreContext> options) : base(options) { }

    public DbSet<StoredEvent> Events { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StoredEvent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(50);
            entity.Property(e => e.EventType).HasMaxLength(100).IsRequired();
            entity.Property(e => e.AggregateId).HasMaxLength(50).IsRequired();
            entity.Property(e => e.EventData).HasColumnType("nvarchar(max)");
            entity.HasIndex(e => new { e.AggregateId, e.Timestamp });
        });
    }
}

public class StoredEvent
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string EventType { get; set; } = null!;
    public string AggregateId { get; set; } = null!;
    public string EventData { get; set; } = null!;
    public DateTime Timestamp { get; set; }
}
