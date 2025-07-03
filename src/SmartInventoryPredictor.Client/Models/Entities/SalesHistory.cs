using System.ComponentModel.DataAnnotations;

namespace SmartInventoryPredictor.Client.Models.Entities;

public class SalesHistory
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public DateTime Date { get; set; }

    public int QuantitySold { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal TotalRevenue => QuantitySold * UnitPrice;

    // Navigation property
    public virtual Product Product { get; set; } = null!;
}