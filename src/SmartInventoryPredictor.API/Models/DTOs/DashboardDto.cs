namespace SmartInventoryPredictor.API.Models.DTOs;

public class DashboardDto
{
    public int TotalProducts { get; set; }
    public int LowStockAlerts { get; set; }
    public decimal TotalInventoryValue { get; set; }
    public decimal MonthlyRevenue { get; set; }
    public List<CategoryStockDto> CategoryStock { get; set; } = new();
    public List<RevenueDataDto> RevenueData { get; set; } = new();
    public List<TopProductDto> TopProducts { get; set; } = new();
}

public class CategoryStockDto
{
    public string Category { get; set; } = string.Empty;
    public int TotalProducts { get; set; }
    public int LowStockCount { get; set; }
    public decimal TotalValue { get; set; }
}

public class RevenueDataDto
{
    public DateTime Date { get; set; }
    public decimal Revenue { get; set; }
}

public class TopProductDto
{
    public string Name { get; set; } = string.Empty;
    public int TotalSold { get; set; }
    public decimal Revenue { get; set; }
}

public class StockMovementDto
{
    public DateTime Date { get; set; }
    public string MovementType { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
}

public class InventoryReportDto
{
    public DateTime GeneratedAt { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public int TotalProducts { get; set; }
    public decimal TotalInventoryValue { get; set; }
    public int LowStockProductsCount { get; set; }
    public int OutOfStockProductsCount { get; set; }
    public decimal TotalSalesInPeriod { get; set; }
    public int TotalUnitsSoldInPeriod { get; set; }
}