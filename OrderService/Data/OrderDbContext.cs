using MassTransit;
using MassTransit.EntityFrameworkCoreIntegration;
using Microsoft.EntityFrameworkCore;
using OrderService.Models;

namespace OrderService.Data;

public class OrderDbContext : DbContext
{
    public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options) { }

    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        //Order table
        modelBuilder.Entity<Order>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.CustomerName).HasMaxLength(200).IsRequired();
            b.Property(x => x.ProductName).HasMaxLength(200).IsRequired();
            b.Property(x => x.TotalPrice).HasPrecision(18, 2);
        });
        //which table is this?
        modelBuilder.AddInboxStateEntity(); //this is for the inbox table used by MassTransit to track consumed messages and ensure idempotency
        modelBuilder.AddOutboxMessageEntity();//MassTransit (events waiting to publish)
        modelBuilder.AddOutboxStateEntity();//MassTransit (delivery tracking)
    }
}