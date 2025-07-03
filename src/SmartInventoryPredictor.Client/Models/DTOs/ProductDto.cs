namespace SmartInventoryPredictor.Client.Models.DTOs;

public class ProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int CurrentStock { get; set; }
    public int MinimumStock { get; set; }
    public bool IsLowStock => CurrentStock <= MinimumStock;
    public string StockStatus => CurrentStock <= MinimumStock ? "Low" :
                                CurrentStock <= MinimumStock * 2 ? "Medium" : "High";
}

public class CreateProductDto
{
    public string Name { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int CurrentStock { get; set; }
    public int MinimumStock { get; set; }
}

public class UpdateProductDto : CreateProductDto
{
    public int Id { get; set; }
}