using SmartInventoryPredictor.Client.Models.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace SmartInventoryPredictor.Client.Models.Entities;

public class PredictionResult
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int ProductId { get; set; }

    [Required]
    public DateTime PredictedDate { get; set; }

    [Required]
    [Column(TypeName = "real")]
    public float PredictedDemand { get; set; }

    [Required]
    [Range(0.0, 1.0)]
    [Column(TypeName = "real")]
    public float Confidence { get; set; }

    [Required]
    [MaxLength(10)]
    public string ABCCategory { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property - ignored during JSON serialization to prevent cycles
    [JsonIgnore]
    public virtual Product? Product { get; set; }
}