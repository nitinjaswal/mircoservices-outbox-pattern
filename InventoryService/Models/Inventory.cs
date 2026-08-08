namespace InventoryService.Models;

public class InventoryItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ProductName { get; set; } = default!;
    public int StockLevel { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

public class InventoryTransaction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrderId { get; set; }
    public string ProductName { get; set; } = default!;
    public int QuantityReserved { get; set; }
    public string CustomerName { get; set; } = default!;
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;

    // Idempotency key — same MessageId from RabbitMQ
    // stored as UNIQUE in DB to prevent double processing
    public string MessageId { get; set; } = default!;
}