using Microsoft.ML;
using Microsoft.ML.Data;
using SmartInventoryPredictor.API.Models.Entities;

namespace SmartInventoryPredictor.API.ML;

public class DemandPredictionModel
{
    private readonly MLContext _mlContext;
    private ITransformer? _model;

    public DemandPredictionModel()
    {
        _mlContext = new MLContext(seed: 0);
    }

    public class SalesData
    {
        public float Year { get; set; }
        public float Month { get; set; }
        public float DayOfYear { get; set; }
        public float ProductId { get; set; }
        public float CategoryHash { get; set; }
        public float UnitPrice { get; set; }
        public float PreviousWeekSales { get; set; }
        public float PreviousMonthSales { get; set; }
        [ColumnName("Label")]
        public float QuantitySold { get; set; }
    }

    public class DemandPrediction
    {
        [ColumnName("Score")]
        public float PredictedDemand { get; set; }
    }

    public async Task<bool> TrainModelAsync(List<SalesHistory> salesHistory, List<Product> products)
    {
        if (!salesHistory.Any()) return false;

        var trainingData = PrepareTrainingData(salesHistory, products);
        var dataView = _mlContext.Data.LoadFromEnumerable(trainingData);

        var pipeline = _mlContext.Transforms.Concatenate("Features",
                "Year", "Month", "DayOfYear", "ProductId", "CategoryHash",
                "UnitPrice", "PreviousWeekSales", "PreviousMonthSales")
            .Append(_mlContext.Regression.Trainers.Sdca(labelColumnName: "Label", featureColumnName: "Features"));

        _model = pipeline.Fit(dataView);
        return true;
    }

    public float PredictDemand(DateTime date, Product product, List<SalesHistory> recentSales)
    {
        if (_model == null) return 0;

        var previousWeekSales = recentSales
            .Where(s => s.ProductId == product.Id && s.Date >= date.AddDays(-7) && s.Date < date)
            .Sum(s => s.QuantitySold);

        var previousMonthSales = recentSales
            .Where(s => s.ProductId == product.Id && s.Date >= date.AddDays(-30) && s.Date < date)
            .Sum(s => s.QuantitySold);

        var input = new SalesData
        {
            Year = date.Year,
            Month = date.Month,
            DayOfYear = date.DayOfYear,
            ProductId = product.Id,
            CategoryHash = (float)product.Category.GetHashCode(),
            UnitPrice = (float)product.UnitPrice,
            PreviousWeekSales = previousWeekSales,
            PreviousMonthSales = previousMonthSales
        };

        var predictionEngine = _mlContext.Model.CreatePredictionEngine<SalesData, DemandPrediction>(_model);
        var prediction = predictionEngine.Predict(input);

        return Math.Max(0, prediction.PredictedDemand);
    }

    private List<SalesData> PrepareTrainingData(List<SalesHistory> salesHistory, List<Product> products)
    {
        var productDict = products.ToDictionary(p => p.Id, p => p);
        var trainingData = new List<SalesData>();

        var groupedSales = salesHistory
            .GroupBy(s => new { s.ProductId, s.Date.Date })
            .Select(g => new
            {
                ProductId = g.Key.ProductId,
                Date = g.Key.Date,
                TotalQuantity = g.Sum(s => s.QuantitySold)
            })
            .OrderBy(s => s.Date)
            .ToList();

        foreach (var sale in groupedSales)
        {
            if (!productDict.TryGetValue(sale.ProductId, out var product)) continue;

            var previousWeekSales = groupedSales
                .Where(s => s.ProductId == sale.ProductId &&
                           s.Date >= sale.Date.AddDays(-7) &&
                           s.Date < sale.Date)
                .Sum(s => s.TotalQuantity);

            var previousMonthSales = groupedSales
                .Where(s => s.ProductId == sale.ProductId &&
                           s.Date >= sale.Date.AddDays(-30) &&
                           s.Date < sale.Date)
                .Sum(s => s.TotalQuantity);

            trainingData.Add(new SalesData
            {
                Year = sale.Date.Year,
                Month = sale.Date.Month,
                DayOfYear = sale.Date.DayOfYear,
                ProductId = sale.ProductId,
                CategoryHash = (float)product.Category.GetHashCode(),
                UnitPrice = (float)product.UnitPrice,
                PreviousWeekSales = previousWeekSales,
                PreviousMonthSales = previousMonthSales,
                QuantitySold = sale.TotalQuantity
            });
        }

        return trainingData;
    }
}