using Microsoft.JSInterop;
 using SmartInventoryPredictor.Client.Models.DTOs;
using SmartInventoryPredictor.Client.Models.Entities;
using System.Net.Http.Json;
 
namespace SmartInventoryPredictor.Client.Services;

public class ApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ApiService> _logger;
    private readonly IJSRuntime _jsRuntime;
    public ApiService(HttpClient httpClient, ILogger<ApiService> logger,IJSRuntime jsRuntime)
    {
        _httpClient = httpClient;
        _logger = logger;
        _jsRuntime = jsRuntime;
    }

    // Dashboard
    public async Task<DashboardDto?> GetDashboardAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<DashboardDto>("/api/inventory/dashboard");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching dashboard data");
            return null;
        }
    }

    // Products
    public async Task<List<ProductDto>> GetProductsAsync(string? category = null, bool? lowStock = null, int page = 1, int pageSize = 20)
    {
        try
        {
            var query = $"/api/products?page={page}&pageSize={pageSize}";
            if (!string.IsNullOrEmpty(category))
                query += $"&category={Uri.EscapeDataString(category)}";
            if (lowStock.HasValue)
                query += $"&lowStock={lowStock.Value}";

            var response = await _httpClient.GetFromJsonAsync<List<ProductDto>>(query);
            return response ?? new List<ProductDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching products");
            return new List<ProductDto>();
        }
    }

    public async Task<ProductDto?> GetProductAsync(int id)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<ProductDto>($"/api/products/{id}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching product {ProductId}", id);
            return null;
        }
    }

    public async Task<bool> CreateProductAsync(CreateProductDto product)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/products", product);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product");
            return false;
        }
    }

    public async Task<bool> UpdateProductAsync(UpdateProductDto product)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"/api/products/{product.Id}", product);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product {ProductId}", product.Id);
            return false;
        }
    }

    public async Task<bool> DeleteProductAsync(int id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"/api/products/{id}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting product {ProductId}", id);
            return false;
        }
    }

    public async Task<List<string>> GetCategoriesAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<List<string>>("/api/products/categories");
            return response ?? new List<string>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching categories");
            return new List<string>();
        }
    }

    // Inventory
    public async Task<List<ProductDto>> GetLowStockProductsAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<List<ProductDto>>("/api/inventory/low-stock");
            return response ?? new List<ProductDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching low stock products");
            return new List<ProductDto>();
        }
    }

    public async Task<bool> UpdateStockAsync(int productId, int newStock)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"/api/inventory/update-stock/{productId}",
                new { NewStock = newStock });
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating stock for product {ProductId}", productId);
            return false;
        }
    }

    // Predictions
    public async Task<bool> GeneratePredictionsAsync()
    {
        try
        {
            var response = await _httpClient.PostAsync("/api/predictions/generate", null);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating predictions");
            return false;
        }
    }

    public async Task<List<PredictionResult>> GetProductPredictionsAsync(int productId)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<List<PredictionResult>>($"/api/predictions/product/{productId}");
            return response ?? new List<PredictionResult>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching predictions for product {ProductId}", productId);
            return new List<PredictionResult>();
        }
    }

    public async Task<bool> ExportPredictionsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/predictions/export");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsByteArrayAsync();
                var fileName = $"predictions_{DateTime.Now:yyyyMMdd}.csv";
                var base64 = Convert.ToBase64String(content);
                await _jsRuntime.InvokeVoidAsync("downloadFile", fileName, base64, "text/csv");
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting predictions");
            return false;
        }
    }


}