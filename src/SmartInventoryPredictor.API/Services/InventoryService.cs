using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SmartInventoryPredictor.API.Data;
using SmartInventoryPredictor.API.Hubs;
using SmartInventoryPredictor.API.Models.DTOs;
using SmartInventoryPredictor.API.Models.Entities;

namespace SmartInventoryPredictor.API.Services;

public class InventoryService : IInventoryService
{
    private readonly ApplicationDbContext _context;
    private readonly IHubContext<InventoryHub> _hubContext;
    private readonly ILogger<InventoryService> _logger;

    public InventoryService(ApplicationDbContext context, IHubContext<InventoryHub> hubContext, ILogger<InventoryService> logger)
    {
        _context = context;
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task<bool> UpdateStockAsync(int productId, int newStock, string reason = "")
    {
        try
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null)
            {
                _logger.LogWarning("Product with ID {ProductId} not found", productId);
                return false;
            }

            var oldStock = product.CurrentStock;
            product.CurrentStock = newStock;
            product.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Log the stock movement
            await LogStockMovementAsync(productId, oldStock, newStock, reason);

            // Send SignalR notification
            await _hubContext.Clients.All.SendAsync("StockUpdated", new
            {
                ProductId = productId,
                ProductName = product.Name,
                OldStock = oldStock,
                NewStock = newStock,
                IsLowStock = newStock <= product.MinimumStock,
                Reason = reason,
                Timestamp = DateTime.UtcNow
            });

            // Send low stock alert if necessary
            if (newStock <= product.MinimumStock)
            {
                var productDto = new ProductDto
                {
                    Id = product.Id,
                    Name = product.Name,
                    SKU = product.SKU,
                    Category = product.Category,
                    UnitPrice = product.UnitPrice,
                    CurrentStock = product.CurrentStock,
                    MinimumStock = product.MinimumStock
                };
                await SendLowStockAlertAsync(productDto);
            }

            _logger.LogInformation("Stock updated for product {ProductId} from {OldStock} to {NewStock}",
                productId, oldStock, newStock);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating stock for product {ProductId}", productId);
            return false;
        }
    }

    public async Task<List<ProductDto>> GetLowStockProductsAsync()
    {
        try
        {
            return await _context.Products
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
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving low stock products");
            return new List<ProductDto>();
        }
    }

    public async Task<DashboardDto> GetDashboardDataAsync()
    {
        try
        {
            var totalProducts = await _context.Products.CountAsync();
            var lowStockProducts = await _context.Products.CountAsync(p => p.CurrentStock <= p.MinimumStock);
            var totalInventoryValue = await _context.Products.SumAsync(p => p.CurrentStock * p.UnitPrice);

            var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
            var monthlyRevenue = await _context.SalesHistory
                .Where(s => s.Date >= thirtyDaysAgo)
                .SumAsync(s => s.QuantitySold * s.UnitPrice); // Calculate revenue in query

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
                .Where(s => s.Date >= thirtyDaysAgo)
                .GroupBy(s => s.Date.Date)
                .Select(g => new RevenueDataDto
                {
                    Date = g.Key,
                    Revenue = g.Sum(s => s.QuantitySold * s.UnitPrice) // Calculate revenue in query
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
                    Revenue = g.Sum(s => s.QuantitySold * s.UnitPrice) // Calculate revenue in query
                })
                .OrderByDescending(p => p.Revenue)
                .Take(10)
                .ToListAsync();

            return new DashboardDto
            {
                TotalProducts = totalProducts,
                LowStockAlerts = lowStockProducts,
                TotalInventoryValue = totalInventoryValue,
                MonthlyRevenue = monthlyRevenue,
                CategoryStock = categoryStock,
                RevenueData = revenueData,
                TopProducts = topProducts
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating dashboard data");
            throw;
        }
    }

    public async Task SendLowStockAlertAsync(ProductDto product)
    {
        try
        {
            await _hubContext.Clients.All.SendAsync("LowStockAlert", new
            {
                ProductId = product.Id,
                ProductName = product.Name,
                SKU = product.SKU,
                CurrentStock = product.CurrentStock,
                MinimumStock = product.MinimumStock,
                Message = $"Low stock alert: {product.Name} has only {product.CurrentStock} units remaining (minimum: {product.MinimumStock})",
                Timestamp = DateTime.UtcNow,
                Severity = product.CurrentStock == 0 ? "Critical" : "Warning"
            });

            _logger.LogWarning("Low stock alert sent for product {ProductId}: {ProductName}",
                product.Id, product.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending low stock alert for product {ProductId}", product.Id);
        }
    }

    public async Task<bool> AdjustStockAsync(int productId, int adjustment, string reason)
    {
        try
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null) return false;

            var newStock = Math.Max(0, product.CurrentStock + adjustment);
            return await UpdateStockAsync(productId, newStock, reason);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adjusting stock for product {ProductId}", productId);
            return false;
        }
    }

    public async Task<List<StockMovementDto>> GetStockMovementsAsync(int productId, int days = 30)
    {
        try
        {
            var startDate = DateTime.UtcNow.AddDays(-days);

            // This would require a StockMovement entity in a real implementation
            // For now, we'll return sales as movements
            return await _context.SalesHistory
                .Where(s => s.ProductId == productId && s.Date >= startDate)
                .Select(s => new StockMovementDto
                {
                    Date = s.Date,
                    MovementType = "Sale",
                    Quantity = -s.QuantitySold, // Negative for outbound
                    Reason = "Product Sale",
                    Reference = $"Sale-{s.Id}"
                })
                .OrderByDescending(s => s.Date)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving stock movements for product {ProductId}", productId);
            return new List<StockMovementDto>();
        }
    }

    public async Task<bool> SetMinimumStockAsync(int productId, int minimumStock)
    {
        try
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null) return false;

            product.MinimumStock = minimumStock;
            product.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Check if current stock is now below minimum
            if (product.CurrentStock <= minimumStock)
            {
                var productDto = new ProductDto
                {
                    Id = product.Id,
                    Name = product.Name,
                    SKU = product.SKU,
                    Category = product.Category,
                    UnitPrice = product.UnitPrice,
                    CurrentStock = product.CurrentStock,
                    MinimumStock = product.MinimumStock
                };
                await SendLowStockAlertAsync(productDto);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting minimum stock for product {ProductId}", productId);
            return false;
        }
    }

    public async Task<InventoryReportDto> GenerateInventoryReportAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        try
        {
            startDate ??= DateTime.UtcNow.AddDays(-30);
            endDate ??= DateTime.UtcNow;

            var products = await _context.Products.ToListAsync();
            var salesInPeriod = await _context.SalesHistory
                .Where(s => s.Date >= startDate && s.Date <= endDate)
                .ToListAsync();

            var report = new InventoryReportDto
            {
                GeneratedAt = DateTime.UtcNow,
                PeriodStart = startDate.Value,
                PeriodEnd = endDate.Value,
                TotalProducts = products.Count,
                TotalInventoryValue = products.Sum(p => p.CurrentStock * p.UnitPrice),
                LowStockProductsCount = products.Count(p => p.CurrentStock <= p.MinimumStock),
                OutOfStockProductsCount = products.Count(p => p.CurrentStock == 0),
                TotalSalesInPeriod = salesInPeriod.Sum(s => s.QuantitySold * s.UnitPrice), // Calculate in memory
                TotalUnitsSoldInPeriod = salesInPeriod.Sum(s => s.QuantitySold)
            };

            return report;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating inventory report");
            throw;
        }
    }

    private async Task LogStockMovementAsync(int productId, int oldStock, int newStock, string reason)
    {
        try
        {
            // In a real implementation, you would have a StockMovement entity
            // For now, we'll just log it
            _logger.LogInformation("Stock movement: Product {ProductId}, {OldStock} -> {NewStock}, Reason: {Reason}",
                productId, oldStock, newStock, reason);

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging stock movement for product {ProductId}", productId);
        }
    }
}