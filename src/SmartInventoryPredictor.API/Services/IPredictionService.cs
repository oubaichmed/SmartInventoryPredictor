using Microsoft.EntityFrameworkCore;
using SmartInventoryPredictor.API.Models.Entities;
 
namespace SmartInventoryPredictor.API.Services;

public interface IPredictionService
{
    Task<List<PredictionResult>> GeneratePredictionsAsync();
    Task<List<PredictionResult>> GetPredictionsAsync(int productId);
    Task<List<PredictionResult>> GetAllPredictionsAsync();
    Task<string> GetABCCategoryAsync(int productId);
    Task<bool> RetrainModelAsync();
    Task<PredictionSummaryDto> GetPredictionSummaryAsync();
    Task<List<PredictionResult>> GetPredictionsByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<List<PredictionResult>> GetHighConfidencePredictionsAsync(float minimumConfidence = 0.8f);
    Task<ModelPerformanceDto> GetModelPerformanceAsync();
    Task<bool> ValidateModelAccuracyAsync();
    Task<List<SeasonalPatternDto>> GetSeasonalPatternsAsync(int productId);
}