using Microsoft.EntityFrameworkCore;
using TransactionService.Models;

namespace TransactionService.Data;

public class TransactionDbContext : DbContext
{
    public TransactionDbContext(DbContextOptions<TransactionDbContext> options) : base(options) { }

    public DbSet<Transaction> Transactions { get; set; } = null!;
    public DbSet<OutboxMessage> OutboxMessages { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(50);
            entity.Property(e => e.AccountId).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Type).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
            entity.HasIndex(e => e.AccountId);
            entity.HasIndex(e => e.CreatedAt);
        });

        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(50);
            entity.Property(e => e.EventType).HasMaxLength(100).IsRequired();
            entity.Property(e => e.AggregateId).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Payload).HasColumnType("nvarchar(max)");
            entity.Property(e => e.Error).HasMaxLength(500);
            entity.HasIndex(e => new { e.Status, e.CreatedAt });
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
        });
    }
}
