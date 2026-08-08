using MassTransit;
using Microsoft.EntityFrameworkCore;
using InventoryService.Data;
using OrderService.Events;
using InventoryService.Models;

namespace InventoryService.Services;

/// <summary>
/// MassTransit consumer — replaces our entire manual consumer.
/// No manual ACK/NACK, no manual connection, no manual retry.
/// MassTransit handles all of that automatically.
/// </summary>
public class OrderPlacedConsumer : IConsumer<OrderPlacedEvent>
{
    private readonly InventoryDbContext _db;
    private readonly ILogger<OrderPlacedConsumer> _logger;

    public OrderPlacedConsumer(InventoryDbContext db, ILogger<OrderPlacedConsumer> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderPlacedEvent> context)
    {
        //_logger.LogInformation(
        // "Consume called for OrderId={OrderId}, MessageId={MessageId}, RedeliveryCount={RedeliveryCount}",
        // context.Message.OrderId,
        // context.MessageId,
        // context.GetRedeliveryCount()); // <-- this is the key one
        //throw new InvalidOperationException("Simulated poison message");
        var evt = context.Message;
        var messageId = context.MessageId.ToString()!;

        _logger.LogInformation("Received OrderPlaced — OrderId: {OrderId} MessageId: {MessageId}",
            evt.OrderId, messageId);

        // ✅ IDEMPOTENCY CHECK
        // MassTransit provides MessageId automatically
        var alreadyProcessed = await _db.Transactions
            .AnyAsync(t => t.MessageId == messageId);

        if (alreadyProcessed)
        {
            _logger.LogWarning("Duplicate message {MessageId} — discarding", messageId);
            return; // MassTransit auto ACKs on normal return
        }

        // Find or create inventory item
        var item = await _db.InventoryItems
            .FirstOrDefaultAsync(i => i.ProductName == evt.ProductName);

        if (item == null)
        {
            item = new InventoryItem
            {
                ProductName = evt.ProductName,
                StockLevel = 100
            };
            _db.InventoryItems.Add(item);
        }

        // Deduct stock
        item.StockLevel -= evt.Quantity;
        item.LastUpdated = DateTime.UtcNow;

        // Record transaction — idempotency key
        _db.Transactions.Add(new InventoryTransaction
        {
            OrderId = evt.OrderId,
            ProductName = evt.ProductName,
            QuantityReserved = evt.Quantity,
            CustomerName = evt.CustomerName,
            MessageId = messageId
        });

        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Stock updated: -{Qty}x {Product}. New level: {Stock}",
            evt.Quantity, evt.ProductName, item.StockLevel);

        // ✅ No BasicAck needed — MassTransit handles it automatically!
        // ✅ No try/catch needed — MassTransit handles retry on exception!
    }
}