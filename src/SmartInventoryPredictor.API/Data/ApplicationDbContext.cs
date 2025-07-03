using Microsoft.EntityFrameworkCore;
using SmartInventoryPredictor.API.Models.Entities;

namespace SmartInventoryPredictor.API.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Product> Products { get; set; }
    public DbSet<SalesHistory> SalesHistory { get; set; }
    public DbSet<PredictionResult> PredictionResults { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Product configuration
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.SKU).IsUnique();
            entity.Property(e => e.UnitPrice).HasPrecision(18, 2);
        });

        // SalesHistory configuration
        modelBuilder.Entity<SalesHistory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UnitPrice).HasPrecision(18, 2);
            entity.HasOne(e => e.Product)
                  .WithMany(p => p.SalesHistory)
                  .HasForeignKey(e => e.ProductId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // PredictionResult configuration
        modelBuilder.Entity<PredictionResult>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Product)
                  .WithMany(p => p.PredictionResults)
                  .HasForeignKey(e => e.ProductId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}