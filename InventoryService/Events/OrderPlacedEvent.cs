namespace OrderService.Events;
public class OrderPlacedEvent
{
    public Guid OrderId { get; set; }
    public string CustomerName { get; set; } = default!;
    public string ProductName { get; set; } = default!;
    public int Quantity { get; set; }
    public decimal TotalPrice { get; set; }
    public DateTime OccurredAt { get; set; }
}