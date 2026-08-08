using Humanizer;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.Events;
using OrderService.Models;
using OrderService.Services;
using Polly.CircuitBreaker;

namespace OrderService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    int GetValue(string a) { return 1; }
    int GetValue(int a) { return 0; }

    private readonly OrderDbContext _db;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<OrdersController> _logger;
    private static int _failureCount = 0;
    private static bool _simulateFailure = false;

    public OrdersController(
        OrderDbContext db,
        IPublishEndpoint publishEndpoint,
        ILogger<OrdersController> logger)
    {
        _db = db;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    [HttpPost("broken")]
    public async Task<IActionResult> CreateOrderBroken([FromBody] CreateOrderRequest req)
    {
        _logger.LogWarning("BROKEN MODE: no atomicity guarantee");

        var order = new Order
        {
            CustomerName = req.CustomerName,
            ProductName = req.ProductName,
            Quantity = req.Quantity,
            TotalPrice = req.Quantity * req.UnitPrice
        };

        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        if (req.SimulateFailure)
        {
            order.Status = "PublishFailed";
            await _db.SaveChangesAsync();
            return StatusCode(500, new
            {
                error = "Broker unavailable",
                message = $"Order {order.Id} saved but event LOST!",
                orderId = order.Id,
                mode = "broken"
            });
        }

        var evt = new OrderPlacedEvent
        {
            OrderId = order.Id,
            CustomerName = order.CustomerName,
            ProductName = order.ProductName,
            Quantity = order.Quantity,
            TotalPrice = order.TotalPrice
        };

        await _publishEndpoint.Publish(evt);
        order.Status = "Published";
        await _db.SaveChangesAsync();

        return Ok(new { orderId = order.Id, mode = "broken", status = "saved_and_published" });
    }

    [HttpPost("fixed")]
    public async Task<IActionResult> CreateOrderFixed([FromBody] CreateOrderRequest req)
    {
        _logger.LogInformation("FIXED MODE: Using MassTransit Outbox");

        var order = new Order
        {
            CustomerName = req.CustomerName,
            ProductName = req.ProductName,
            Quantity = req.Quantity,
            TotalPrice = req.Quantity * req.UnitPrice,
            Status = "Pending"
        };

        var evt = new OrderPlacedEvent
        {
            OrderId = order.Id,
            CustomerName = order.CustomerName,
            ProductName = order.ProductName,
            Quantity = order.Quantity,
            TotalPrice = order.TotalPrice
        };

        try
        {
            //  MassTransit outbox intercepts Publish
            // stores in OutboxMessage table atomically
            // with SaveChangesAsync

            //how data is stored in the database when using MassTransit outbox pattern
            //How data gets inserted into OutboxMessage table:
            _db.Orders.Add(order);
            // save changes to the database, which will also save the outboxmessage and outboxstate
            // MassTransit will intercept the Publish call and store the event in the OutboxMessage table
            //json event is stored in the OutboxMessage table, and the OutboxState table keeps track of the state of the outbox message
            await _publishEndpoint.Publish(evt);
            //.publish will 

            await _db.SaveChangesAsync();


            _logger.LogInformation("Order {Id} saved and outbox message stored", order.Id);

            return Ok(new
            {
                orderId = order.Id,
                mode = "masstransit_outbox",
                status = "atomically_saved"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save order: {Message}", ex.Message);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetOrders()
    {
        var policy = ResiliencePolicies.GetCombinedPolicy(_logger);

        try
        {
            var orders = await policy.ExecuteAsync(async () =>
            {
                if (_simulateFailure)
                {
                    _failureCount++;
                    _logger.LogWarning("Simulated DB failure #{Count}", _failureCount);
                    throw new Exception($"Simulated DB failure #{_failureCount}");
                }

                return await _db.Orders
                    .OrderByDescending(o => o.CreatedAt)
                    .Take(50)
                    .ToListAsync();
            });

            return Ok(new
            {
                source = "database",
                count = orders.Count,
                orders
            });
        }
        catch (BrokenCircuitException ex)
        {
            _logger.LogError("Circuit is OPEN — fast failing");
            return StatusCode(503, new
            {
                error = "Service temporarily unavailable",
                reason = "Circuit breaker is OPEN",
                message = ex.Message,
                retryAfter = "10 seconds"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError("All retries exhausted: {Message}", ex.Message);
            return StatusCode(500, new
            {
                error = "All retries exhausted",
                message = ex.Message
            });
        }
    }

    [HttpPost("simulate-db-failure/on")]
    public IActionResult TurnOnFailure()
    {
        _simulateFailure = true;
        _failureCount = 0;
        _logger.LogWarning("DB failure simulation ENABLED");
        return Ok(new { simulation = "ON", message = "DB calls will now fail" });
    }

    [HttpPost("simulate-db-failure/off")]
    public IActionResult TurnOffFailure()
    {
        _simulateFailure = false;
        _failureCount = 0;
        _logger.LogInformation("DB failure simulation DISABLED");
        return Ok(new { simulation = "OFF", message = "DB calls will succeed again" });
    }

    [HttpGet("circuit-status")]
    public IActionResult CircuitStatus()
    {
        return Ok(new
        {
            simulatingFailure = _simulateFailure,
            failureCount = _failureCount,
            message = _simulateFailure
                ? "DB failures active — circuit may be open"
                : "DB working normally"
        });
    }
}

public record CreateOrderRequest(
    string CustomerName,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    bool SimulateFailure = false
);