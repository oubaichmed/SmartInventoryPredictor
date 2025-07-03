using System.ComponentModel.DataAnnotations;

namespace SmartInventoryPredictor.Client.Models.Entities;

public class Product
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string SKU { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string Category { get; set; } = string.Empty;

    public decimal UnitPrice { get; set; }

    public int CurrentStock { get; set; }

    public int MinimumStock { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ICollection<SalesHistory> SalesHistory { get; set; } = new List<SalesHistory>();
    public virtual ICollection<PredictionResult> PredictionResults { get; set; } = new List<PredictionResult>();
}