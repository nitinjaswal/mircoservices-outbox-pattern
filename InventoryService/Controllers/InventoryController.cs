using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InventoryService.Data;

namespace InventoryService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InventoryController : ControllerBase
{
    private readonly InventoryDbContext _db;

    public InventoryController(InventoryDbContext db) => _db = db;

    // Current stock levels for all products
    [HttpGet]
    public async Task<IActionResult> GetInventory() =>
        Ok(await _db.InventoryItems
            .OrderBy(i => i.ProductName)
            .ToListAsync());

    // All processed transactions
    [HttpGet("transactions")]
    public async Task<IActionResult> GetTransactions() =>
        Ok(await _db.Transactions
            .OrderByDescending(t => t.ProcessedAt)
            .Take(50)
            .ToListAsync());

}