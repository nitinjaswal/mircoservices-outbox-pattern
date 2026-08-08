namespace OrderService.Models;

public class Order
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string CustomerName { get; set; } = default!;
    public string ProductName { get; set; } = default!;
    public int Quantity { get; set; }
    public decimal TotalPrice { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

//public class OutboxMessage
//{
//    public Guid Id { get; set; } = Guid.NewGuid();
//    public string Type { get; set; } = default!;       // event class name
//    public string Payload { get; set; } = default!;    // JSON serialized event
//    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
//    public DateTime? ProcessedAt { get; set; }         // null = not yet published
//    public int RetryCount { get; set; } = 0;
//    public string? Error { get; set; }
//    public string MessageId { get; set; } = Guid.NewGuid().ToString();
//}

// order and outbox message are stored in the same database transaction to ensure atomicity