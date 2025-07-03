using SmartInventoryPredictor.API.Models.DTOs;

namespace SmartInventoryPredictor.API.Services;

public interface IInventoryService
{
    Task<bool> UpdateStockAsync(int productId, int newStock, string reason = "");
    Task<List<ProductDto>> GetLowStockProductsAsync();
    Task<DashboardDto> GetDashboardDataAsync();
    Task SendLowStockAlertAsync(ProductDto product);
    Task<bool> AdjustStockAsync(int productId, int adjustment, string reason);
    Task<List<StockMovementDto>> GetStockMovementsAsync(int productId, int days = 30);
    Task<bool> SetMinimumStockAsync(int productId, int minimumStock);
    Task<InventoryReportDto> GenerateInventoryReportAsync(DateTime? startDate = null, DateTime? endDate = null);
}