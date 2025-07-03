using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartInventoryPredictor.API.Data;
using SmartInventoryPredictor.API.Models.DTOs;
using SmartInventoryPredictor.API.Models.Entities;

namespace SmartInventoryPredictor.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(ApplicationDbContext context, ILogger<ProductsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetProducts(
        [FromQuery] string? category = null,
        [FromQuery] bool? lowStock = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            var query = _context.Products.AsQueryable();

            if (!string.IsNullOrEmpty(category))
                query = query.Where(p => p.Category == category);

            if (lowStock.HasValue && lowStock.Value)
                query = query.Where(p => p.CurrentStock <= p.MinimumStock);

            var totalCount = await query.CountAsync();
            var products = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new ProductDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    SKU = p.SKU,
                    Category = p.Category,
                    UnitPrice = p.UnitPrice,
                    CurrentStock = p.CurrentStock,
                    MinimumStock = p.MinimumStock,
                    StockStatus = GetStockStatus(p.CurrentStock, p.MinimumStock),
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt
                })
                .ToListAsync();

            Response.Headers.Add("X-Total-Count", totalCount.ToString());
            return Ok(products);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving products");
            return StatusCode(500, "Error retrieving products");
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ProductDto>> GetProduct(int id)
    {
        try
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return NotFound();

            return Ok(new ProductDto
            {
                Id = product.Id,
                Name = product.Name,
                SKU = product.SKU,
                Category = product.Category,
                UnitPrice = product.UnitPrice,
                CurrentStock = product.CurrentStock,
                MinimumStock = product.MinimumStock,
                StockStatus = GetStockStatus(product.CurrentStock, product.MinimumStock),
                CreatedAt = product.CreatedAt,
                UpdatedAt = product.UpdatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving product {ProductId}", id);
            return StatusCode(500, "Error retrieving product");
        }
    }

    [HttpPost]
    public async Task<ActionResult<ProductDto>> CreateProduct(CreateProductDto dto)
    {
        try
        {
            if (await _context.Products.AnyAsync(p => p.SKU == dto.SKU))
                return BadRequest("SKU already exists");

            var product = new Product
            {
                Name = dto.Name,
                SKU = dto.SKU,
                Category = dto.Category,
                UnitPrice = dto.UnitPrice,
                CurrentStock = dto.CurrentStock,
                MinimumStock = dto.MinimumStock,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            var result = new ProductDto
            {
                Id = product.Id,
                Name = product.Name,
                SKU = product.SKU,
                Category = product.Category,
                UnitPrice = product.UnitPrice,
                CurrentStock = product.CurrentStock,
                MinimumStock = product.MinimumStock,
                StockStatus = GetStockStatus(product.CurrentStock, product.MinimumStock),
                CreatedAt = product.CreatedAt,
                UpdatedAt = product.UpdatedAt
            };

            return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product");
            return StatusCode(500, "Error creating product");
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateProduct(int id, UpdateProductDto dto)
    {
        try
        {
            if (id != dto.Id)
                return BadRequest("ID mismatch");

            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return NotFound();

            if (await _context.Products.AnyAsync(p => p.SKU == dto.SKU && p.Id != id))
                return BadRequest("SKU already exists");

            product.Name = dto.Name;
            product.SKU = dto.SKU;
            product.Category = dto.Category;
            product.UnitPrice = dto.UnitPrice;
            product.CurrentStock = dto.CurrentStock;
            product.MinimumStock = dto.MinimumStock;
            product.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product {ProductId}", id);
            return StatusCode(500, "Error updating product");
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteProduct(int id)
    {
        try
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return NotFound();

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting product {ProductId}", id);
            return StatusCode(500, "Error deleting product");
        }
    }

    [HttpGet("categories")]
    public async Task<ActionResult<IEnumerable<string>>> GetCategories()
    {
        try
        {
            var categories = await _context.Products
                .Select(p => p.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            // Add some default categories if none exist
            if (!categories.Any())
            {
                categories = new List<string>
                {
                    "Electronics",
                    "Clothing",
                    "Books",
                    "Home & Garden",
                    "Sports",
                    "Toys"
                };
            }

            return Ok(categories);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving categories");
            return StatusCode(500, "Error retrieving categories");
        }
    }

    [HttpPost("{id}/update-stock")]
    public async Task<ActionResult> UpdateStock(int id, [FromBody] UpdateStockRequest request)
    {
        try
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return NotFound();

            product.CurrentStock = request.NewStock;
            product.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Stock updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating stock for product {ProductId}", id);
            return StatusCode(500, "Error updating stock");
        }
    }

    private static string GetStockStatus(int currentStock, int minimumStock)
    {
        if (currentStock == 0) return "Out of Stock";
        if (currentStock <= minimumStock) return "Low";
        if (currentStock <= minimumStock * 2) return "Medium";
        return "High";
    }
}

public class UpdateStockRequest
{
    public int NewStock { get; set; }
}