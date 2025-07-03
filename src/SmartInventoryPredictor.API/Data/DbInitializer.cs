using SmartInventoryPredictor.API.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace SmartInventoryPredictor.API.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        if (await context.Products.AnyAsync())
            return; // Database has been seeded

        var categories = new[] { "Electronics", "Clothing", "Books", "Home & Garden", "Sports", "Toys" };
        var random = new Random(42); // Fixed seed for reproducible data

        var products = new List<Product>();

        // Generate 50 products
        for (int i = 1; i <= 50; i++)
        {
            var category = categories[random.Next(categories.Length)];
            var product = new Product
            {
                Name = GenerateProductName(category, i),
                SKU = $"SKU{i:D4}",
                Category = category,
                UnitPrice = (decimal)(random.NextDouble() * 500 + 10), // $10 - $510
                CurrentStock = random.Next(0, 1000),
                MinimumStock = random.Next(10, 50)
            };
            products.Add(product);
        }

        await context.Products.AddRangeAsync(products);
        await context.SaveChangesAsync();

        // Generate 2 years of sales history
        var salesHistory = new List<SalesHistory>();
        var startDate = DateTime.UtcNow.AddYears(-2);

        foreach (var product in products)
        {
            var baselineDaily = random.Next(1, 20); // Base daily sales

            for (var date = startDate; date <= DateTime.UtcNow; date = date.AddDays(1))
            {
                // Add seasonal patterns
                var seasonalMultiplier = GetSeasonalMultiplier(date, product.Category);
                var dailySales = (int)(baselineDaily * seasonalMultiplier * (0.5 + random.NextDouble()));

                if (dailySales > 0)
                {
                    var sale = new SalesHistory
                    {
                        ProductId = product.Id,
                        Date = date,
                        QuantitySold = dailySales,
                        UnitPrice = product.UnitPrice * (decimal)(0.9 + random.NextDouble() * 0.2) // ±10% price variation
                    };
                    salesHistory.Add(sale);
                }
            }
        }

        await context.SalesHistory.AddRangeAsync(salesHistory);
        await context.SaveChangesAsync();
    }

    private static string GenerateProductName(string category, int index)
    {
        var names = category switch
        {
            "Electronics" => new[] { "Smartphone", "Laptop", "Tablet", "Headphones", "Smart Watch", "Camera", "Gaming Console", "Bluetooth Speaker" },
            "Clothing" => new[] { "T-Shirt", "Jeans", "Jacket", "Sneakers", "Dress", "Hoodie", "Pants", "Shirt" },
            "Books" => new[] { "Fiction Novel", "Science Book", "Biography", "Cookbook", "Travel Guide", "Self-Help", "Mystery Novel", "Technical Manual" },
            "Home & Garden" => new[] { "Plant Pot", "Garden Tool", "Home Decor", "Kitchen Appliance", "Furniture", "Light Fixture", "Storage Box", "Cleaning Supply" },
            "Sports" => new[] { "Basketball", "Soccer Ball", "Tennis Racket", "Yoga Mat", "Dumbbells", "Running Shoes", "Bicycle", "Swimming Goggles" },
            "Toys" => new[] { "Action Figure", "Board Game", "Puzzle", "Remote Control Car", "Doll", "Building Blocks", "Art Set", "Musical Toy" },
            _ => new[] { "Generic Product" }
        };

        var baseName = names[index % names.Length];
        return $"{baseName} {char.ToUpper((char)('A' + (index % 26)))}";
    }

    private static double GetSeasonalMultiplier(DateTime date, string category)
    {
        var month = date.Month;

        return category switch
        {
            "Electronics" => month is 11 or 12 or 1 ? 1.5 : 1.0, // Holiday boost
            "Clothing" => month is 3 or 4 or 9 or 10 ? 1.3 : 1.0, // Spring/Fall seasons
            "Sports" => month is >= 4 and <= 9 ? 1.4 : 0.7, // Summer boost
            "Toys" => month is 11 or 12 ? 2.0 : month is 6 or 7 ? 1.2 : 1.0, // Holiday + summer
            _ => 1.0
        };
    }
}