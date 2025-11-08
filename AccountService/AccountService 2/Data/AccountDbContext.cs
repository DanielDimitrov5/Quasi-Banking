using Microsoft.EntityFrameworkCore;
using AccountService.Models;

namespace AccountService.Data;

public class AccountDbContext : DbContext
{
    public AccountDbContext(DbContextOptions<AccountDbContext> options) : base(options) { }

    public DbSet<Account> Accounts { get; set; } = null!;
    public DbSet<OutboxMessage> OutboxMessages { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(50);
            entity.Property(e => e.OwnerId).HasMaxLength(50).IsRequired();
            entity.Property(e => e.OwnerEmail).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Balance).HasColumnType("decimal(18,2)");
            entity.HasIndex(e => e.OwnerId);
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
