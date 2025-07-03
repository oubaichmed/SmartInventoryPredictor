namespace SmartInventoryPredictor.Client.Models.DTOs;

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