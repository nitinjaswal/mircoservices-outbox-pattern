using OrderService.Models;

namespace OrderService.Events;

public class OrderPlacedEvent
{
    public Guid OrderId { get; set; }
    public string CustomerName { get; set; } = default!;
    public string ProductName { get; set; } = default!;
    public int Quantity { get; set; }
    public decimal TotalPrice { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}

//Why is this a separate class from Order?
// This is an event class that represents a specific occurrence in the system.
// It is separate from the Order class because it is used for event-driven communication,
// not for storing the state of an order in the database.

//Order is your domain model — it belongs to the database, can have navigation properties,
//EF annotations etc.

//OrderPlacedEvent is a message contract — it's what gets serialized to JSON and sent over the wire to other services. You want full control over its shape independently of your DB model.
//If you change your Order table schema later, you don't want to accidentally break the event contract that InventoryService depends on.

//Why OccurredAt instead of CreatedAt?

//Convention in event-driven systems — OccurredAt means "when did this business thing happen", which is semantically different from "when was this DB row created".

//This class will be copied to InventoryService too — both services need to agree on the same shape to serialize/deserialize correctly.