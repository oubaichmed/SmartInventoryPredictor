using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SmartInventoryPredictor.API.Data;
using SmartInventoryPredictor.API.Hubs;
using SmartInventoryPredictor.API.Models.DTOs;

namespace SmartInventoryPredictor.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InventoryController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IHubContext<InventoryHub> _hubContext;
    private readonly ILogger<InventoryController> _logger;

    public InventoryController(ApplicationDbContext context, IHubContext<InventoryHub> hubContext, ILogger<InventoryController> logger)
    {
        _context = context;
        _hubContext = hubContext;
        _logger = logger;
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<DashboardDto>> GetDashboard()
    {
        var totalProducts = await _context.Products.CountAsync();
        var lowStockProducts = await _context.Products.CountAsync(p => p.CurrentStock <= p.MinimumStock);

        var totalInventoryValue = await _context.Products
            .SumAsync(p => p.CurrentStock * p.UnitPrice);

        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
        var monthlyRevenue = await _context.SalesHistory
            .Where(s => s.Date >= thirtyDaysAgo)
            .SumAsync(s => s.TotalRevenue);

        var categoryStock = await _context.Products
            .GroupBy(p => p.Category)
            .Select(g => new CategoryStockDto
            {
                Category = g.Key,
                TotalProducts = g.Count(),
                LowStockCount = g.Count(p => p.CurrentStock <= p.MinimumStock),
                TotalValue = g.Sum(p => p.CurrentStock * p.UnitPrice)
            })
            .ToListAsync();

        var revenueData = await _context.SalesHistory
            .Where(s => s.Date >= DateTime.UtcNow.AddDays(-30))
            .GroupBy(s => s.Date.Date)
            .Select(g => new RevenueDataDto
            {
                Date = g.Key,
                Revenue = g.Sum(s => s.TotalRevenue)
            })
            .OrderBy(r => r.Date)
            .ToListAsync();

        var topProducts = await _context.SalesHistory
            .Where(s => s.Date >= thirtyDaysAgo)
            .GroupBy(s => s.Product.Name)
            .Select(g => new TopProductDto
            {
                Name = g.Key,
                TotalSold = g.Sum(s => s.QuantitySold),
                Revenue = g.Sum(s => s.TotalRevenue)
            })
            .OrderByDescending(p => p.Revenue)
            .Take(10)
            .ToListAsync();

        return Ok(new DashboardDto
        {
            TotalProducts = totalProducts,
            LowStockAlerts = lowStockProducts,
            TotalInventoryValue = totalInventoryValue,
            MonthlyRevenue = monthlyRevenue,
            CategoryStock = categoryStock,
            RevenueData = revenueData,
            TopProducts = topProducts
        });
    }

    [HttpPost("update-stock/{productId}")]
    public async Task<ActionResult> UpdateStock(int productId, [FromBody] UpdateStockDto dto)
    {
        var product = await _context.Products.FindAsync(productId);
        if (product == null)
            return NotFound();

        var oldStock = product.CurrentStock;
        product.CurrentStock = dto.NewStock;
        product.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Send real-time notification
        await _hubContext.Clients.All.SendAsync("StockUpdated", new
        {
            ProductId = productId,
            ProductName = product.Name,
            OldStock = oldStock,
            NewStock = dto.NewStock,
            IsLowStock = dto.NewStock <= product.MinimumStock
        });

        return Ok();
    }

    [HttpGet("low-stock")]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetLowStockProducts()
    {
        var products = await _context.Products
            .Where(p => p.CurrentStock <= p.MinimumStock)
            .Select(p => new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                SKU = p.SKU,
                Category = p.Category,
                UnitPrice = p.UnitPrice,
                CurrentStock = p.CurrentStock,
                MinimumStock = p.MinimumStock
            })
            .ToListAsync();

        return Ok(products);
    }
}

public class UpdateStockDto
{
    public int NewStock { get; set; }
}