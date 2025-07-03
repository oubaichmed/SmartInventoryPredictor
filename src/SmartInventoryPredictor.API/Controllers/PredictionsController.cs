using Microsoft.AspNetCore.Mvc;
using SmartInventoryPredictor.API.Services;
using SmartInventoryPredictor.API.Models.Entities;
using System.Text;
using CsvHelper;
using System.Globalization;

namespace SmartInventoryPredictor.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PredictionsController : ControllerBase
{
    private readonly IPredictionService _predictionService;
    private readonly ILogger<PredictionsController> _logger;

    public PredictionsController(IPredictionService predictionService, ILogger<PredictionsController> logger)
    {
        _predictionService = predictionService;
        _logger = logger;
    }

    [HttpPost("generate")]
    public async Task<ActionResult<IEnumerable<PredictionResult>>> GeneratePredictions()
    {
        var predictions = await _predictionService.GeneratePredictionsAsync();
        return Ok(predictions);
    }

    [HttpGet("product/{productId}")]
    public async Task<ActionResult<IEnumerable<PredictionResult>>> GetProductPredictions(int productId)
    {
        var predictions = await _predictionService.GetPredictionsAsync(productId);
        return Ok(predictions);
    }

    [HttpGet("export")]
    public async Task<ActionResult> ExportPredictions()
    {
        try
        {
            var predictions = await _predictionService.GeneratePredictionsAsync();

            using var memoryStream = new MemoryStream();
            using var writer = new StreamWriter(memoryStream);
            using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

            csv.WriteRecords(predictions.Select(p => new
            {
                ProductId = p.ProductId,
                PredictedDate = p.PredictedDate.ToString("yyyy-MM-dd"),
                PredictedDemand = p.PredictedDemand,
                Confidence = p.Confidence,
                ABCCategory = p.ABCCategory
            }));

            writer.Flush();
            var result = memoryStream.ToArray();

            return File(result, "text/csv", $"predictions_{DateTime.UtcNow:yyyyMMdd}.csv");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting predictions");
            return StatusCode(500, "Error generating export file");
        }
    }
}