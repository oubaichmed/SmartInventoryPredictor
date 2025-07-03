using Microsoft.ML;
using Microsoft.ML.Data;
using SmartInventoryPredictor.API.Models.Entities;

namespace SmartInventoryPredictor.API.ML;

public class ABCAnalysisModel
{
    private readonly MLContext _mlContext;
    private readonly ILogger<ABCAnalysisModel>? _logger;

    public ABCAnalysisModel(ILogger<ABCAnalysisModel>? logger = null)
    {
        _mlContext = new MLContext(seed: 0);
        _logger = logger;
    }

    public class ProductAnalysisData
    {
        public float Revenue { get; set; }
        public float Volume { get; set; }
        public float Frequency { get; set; }
        public float UnitPrice { get; set; }
        public float AverageOrderValue { get; set; }
        public float SeasonalityIndex { get; set; }

        [ColumnName("Label")]
        public string Category { get; set; } = string.Empty;
    }

    public class CategoryPrediction
    {
        [ColumnName("PredictedLabel")]
        public string PredictedCategory { get; set; } = string.Empty;

        [ColumnName("Score")]
        public float[] Score { get; set; } = Array.Empty<float>();
    }

    public string ClassifyProduct(decimal revenue, int volume, int frequency, decimal unitPrice,
        decimal averageOrderValue = 0, double seasonalityIndex = 1.0)
    {
        try
        {
            // Calculate component scores
            var revenueScore = CalculateRevenueScore(revenue);
            var volumeScore = CalculateVolumeScore(volume);
            var frequencyScore = CalculateFrequencyScore(frequency);
            var priceScore = CalculatePriceScore(unitPrice);
            var aovScore = CalculateAOVScore(averageOrderValue);
            var seasonalScore = CalculateSeasonalScore(seasonalityIndex);

            // Weighted scoring algorithm
            var totalScore = (revenueScore * 0.35) +     // Revenue is most important
                           (volumeScore * 0.25) +        // Volume second
                           (frequencyScore * 0.20) +     // Frequency third
                           (priceScore * 0.10) +         // Unit price
                           (aovScore * 0.05) +           // Average order value
                           (seasonalScore * 0.05);       // Seasonality factor

            return totalScore switch
            {
                >= 0.75 => "A", // High value items (top 20%)
                >= 0.45 => "B", // Medium value items (next 30%)
                _ => "C"        // Low value items (remaining 50%)
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error classifying product with revenue {Revenue}, volume {Volume}", revenue, volume);
            return "C"; // Default to C on error
        }
    }

    public ABCAnalysisResult AnalyzeProductPortfolio(List<Product> products, List<SalesHistory> salesHistory)
    {
        try
        {
            var analysisDate = DateTime.UtcNow;
            var analysisStartDate = analysisDate.AddDays(-90); // Last 90 days

            var productAnalyses = new List<ProductABCAnalysis>();

            foreach (var product in products)
            {
                var productSales = salesHistory
                    .Where(s => s.ProductId == product.Id && s.Date >= analysisStartDate)
                    .ToList();

                var revenue = productSales.Sum(s => s.TotalRevenue);
                var volume = productSales.Sum(s => s.QuantitySold);
                var frequency = productSales.Count;
                var avgOrderValue = frequency > 0 ? revenue / frequency : 0;
                var seasonalityIndex = CalculateSeasonalityIndex(productSales, analysisDate);

                var category = ClassifyProduct(revenue, volume, frequency, product.UnitPrice, avgOrderValue, seasonalityIndex);

                productAnalyses.Add(new ProductABCAnalysis
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    SKU = product.SKU,
                    Category = product.Category,
                    ABCCategory = category,
                    Revenue = revenue,
                    Volume = volume,
                    Frequency = frequency,
                    UnitPrice = product.UnitPrice,
                    AverageOrderValue = avgOrderValue,
                    SeasonalityIndex = seasonalityIndex,
                    CurrentStock = product.CurrentStock,
                    MinimumStock = product.MinimumStock
                });
            }

            return new ABCAnalysisResult
            {
                AnalysisDate = analysisDate,
                AnalysisPeriodStart = analysisStartDate,
                AnalysisPeriodEnd = analysisDate,
                TotalProducts = products.Count,
                CategoryACount = productAnalyses.Count(p => p.ABCCategory == "A"),
                CategoryBCount = productAnalyses.Count(p => p.ABCCategory == "B"),
                CategoryCCount = productAnalyses.Count(p => p.ABCCategory == "C"),
                CategoryARevenue = productAnalyses.Where(p => p.ABCCategory == "A").Sum(p => p.Revenue),
                CategoryBRevenue = productAnalyses.Where(p => p.ABCCategory == "B").Sum(p => p.Revenue),
                CategoryCRevenue = productAnalyses.Where(p => p.ABCCategory == "C").Sum(p => p.Revenue),
                ProductAnalyses = productAnalyses
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error analyzing product portfolio");
            throw;
        }
    }

    private double CalculateRevenueScore(decimal revenue)
    {
        return revenue switch
        {
            >= 50000 => 1.0,
            >= 20000 => 0.9,
            >= 10000 => 0.8,
            >= 5000 => 0.7,
            >= 2000 => 0.6,
            >= 1000 => 0.5,
            >= 500 => 0.4,
            >= 100 => 0.3,
            >= 50 => 0.2,
            > 0 => 0.1,
            _ => 0.0
        };
    }

    private double CalculateVolumeScore(int volume)
    {
        return volume switch
        {
            >= 5000 => 1.0,
            >= 2000 => 0.9,
            >= 1000 => 0.8,
            >= 500 => 0.7,
            >= 200 => 0.6,
            >= 100 => 0.5,
            >= 50 => 0.4,
            >= 20 => 0.3,
            >= 10 => 0.2,
            > 0 => 0.1,
            _ => 0.0
        };
    }

    private double CalculateFrequencyScore(int frequency)
    {
        return frequency switch
        {
            >= 100 => 1.0,
            >= 75 => 0.9,
            >= 50 => 0.8,
            >= 30 => 0.7,
            >= 20 => 0.6,
            >= 15 => 0.5,
            >= 10 => 0.4,
            >= 5 => 0.3,
            >= 3 => 0.2,
            > 0 => 0.1,
            _ => 0.0
        };
    }

    private double CalculatePriceScore(decimal unitPrice)
    {
        return unitPrice switch
        {
            >= 1000 => 1.0,
            >= 500 => 0.8,
            >= 200 => 0.6,
            >= 100 => 0.5,
            >= 50 => 0.4,
            >= 20 => 0.3,
            >= 10 => 0.2,
            > 0 => 0.1,
            _ => 0.0
        };
    }

    private double CalculateAOVScore(decimal averageOrderValue)
    {
        return averageOrderValue switch
        {
            >= 500 => 1.0,
            >= 200 => 0.8,
            >= 100 => 0.6,
            >= 50 => 0.4,
            >= 20 => 0.2,
            > 0 => 0.1,
            _ => 0.0
        };
    }

    private double CalculateSeasonalScore(double seasonalityIndex)
    {
        // Seasonality index of 1.0 is average, > 1.0 is above average demand
        return seasonalityIndex switch
        {
            >= 2.0 => 1.0,
            >= 1.5 => 0.8,
            >= 1.2 => 0.6,
            >= 0.8 => 0.4,
            >= 0.5 => 0.2,
            _ => 0.0
        };
    }

    private double CalculateSeasonalityIndex(List<SalesHistory> productSales, DateTime analysisDate)
    {
        if (!productSales.Any()) return 1.0;

        // Calculate average sales for the same month in previous years
        var currentMonth = analysisDate.Month;
        var currentMonthSales = productSales
            .Where(s => s.Date.Month == currentMonth)
            .Sum(s => s.QuantitySold);

        var totalSales = productSales.Sum(s => s.QuantitySold);
        var averageMonthlySales = totalSales / 12.0; // Assuming 12 months of data

        return averageMonthlySales > 0 ? currentMonthSales / averageMonthlySales : 1.0;
    }
}

public class ABCAnalysisResult
{
    public DateTime AnalysisDate { get; set; }
    public DateTime AnalysisPeriodStart { get; set; }
    public DateTime AnalysisPeriodEnd { get; set; }
    public int TotalProducts { get; set; }
    public int CategoryACount { get; set; }
    public int CategoryBCount { get; set; }
    public int CategoryCCount { get; set; }
    public decimal CategoryARevenue { get; set; }
    public decimal CategoryBRevenue { get; set; }
    public decimal CategoryCRevenue { get; set; }
    public List<ProductABCAnalysis> ProductAnalyses { get; set; } = new();
}

public class ProductABCAnalysis
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string ABCCategory { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public int Volume { get; set; }
    public int Frequency { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal AverageOrderValue { get; set; }
    public double SeasonalityIndex { get; set; }
    public int CurrentStock { get; set; }
    public int MinimumStock { get; set; }
}