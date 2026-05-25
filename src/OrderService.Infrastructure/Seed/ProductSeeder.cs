using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrderService.Domain.Entities;
using OrderService.Infrastructure.Persistence;

namespace OrderService.Infrastructure.Seed;

public static class ProductSeeder
{
    public static async Task SeedAsync(AppDbContext context, ILogger logger)
    {
        if (await context.Products.AnyAsync())
        {
            logger.LogInformation("Products already seeded. Skipping.");
            return;
        }

        var products = new List<Product>
        {
            new(Guid.Parse("10000000-0000-0000-0000-000000000001"), "Notebook Pro 15",   4999.99m, "BRL", 50),
            new(Guid.Parse("10000000-0000-0000-0000-000000000002"), "Mouse Gamer RGB",    299.90m, "BRL", 200),
            new(Guid.Parse("10000000-0000-0000-0000-000000000003"), "Teclado Mecânico",   599.00m, "BRL", 150),
            new(Guid.Parse("10000000-0000-0000-0000-000000000004"), "Monitor 27\" 4K",  2499.00m, "BRL", 30),
            new(Guid.Parse("10000000-0000-0000-0000-000000000005"), "Headset Wireless",   799.00m, "BRL", 80),
            new(Guid.Parse("10000000-0000-0000-0000-000000000006"), "Webcam Full HD",     349.90m, "BRL", 120),
            new(Guid.Parse("10000000-0000-0000-0000-000000000007"), "SSD 1TB NVMe",       599.90m, "BRL", 100),
            new(Guid.Parse("10000000-0000-0000-0000-000000000008"), "Cadeira Gamer",     1899.00m, "BRL", 25),
        };

        await context.Products.AddRangeAsync(products);
        await context.SaveChangesAsync();

        logger.LogInformation("Seeded {Count} products successfully.", products.Count);
    }
}
