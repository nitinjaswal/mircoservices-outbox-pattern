using Microsoft.EntityFrameworkCore;
using InventoryService.Models;

namespace InventoryService.Data;

public class InventoryDbContext : DbContext
{
    public InventoryDbContext(DbContextOptions<InventoryDbContext> options) : base(options) { }

    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<InventoryTransaction> Transactions => Set<InventoryTransaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InventoryItem>(b =>
        {
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.ProductName).IsUnique();
            b.Property(x => x.ProductName).HasMaxLength(200).IsRequired();
        });

        modelBuilder.Entity<InventoryTransaction>(b =>
        {
            b.HasKey(x => x.Id);
            // UNIQUE on MessageId = DB enforced idempotency
            // Second insert with same MessageId will throw
            // preventing double stock deduction
            b.HasIndex(x => x.MessageId).IsUnique();
            b.Property(x => x.ProductName).HasMaxLength(200).IsRequired();
            b.Property(x => x.CustomerName).HasMaxLength(200).IsRequired();
        });
    }
}