using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrderService.Infrastructure.Persistence;
using OrderService.Infrastructure.Seed;

namespace OrderService.Infrastructure.DependencyInjection;

public static class DatabaseInitializer
{
    /// <summary>
    /// Aplica migrations pendentes e executa seed de dados.
    /// Chamado no startup da API antes de processar requests.
    /// </summary>
    public static async Task InitializeDatabaseAsync(this IHost app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<AppDbContext>>();

        try
        {
            var context = services.GetRequiredService<AppDbContext>();

            logger.LogInformation("Applying pending migrations...");
            await context.Database.MigrateAsync();
            logger.LogInformation("Migrations applied successfully.");

            await ProductSeeder.SeedAsync(context, logger);
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "An error occurred during database initialization.");
            throw; // Falha no startup é fatal — não sobe a API com banco inconsistente
        }
    }
}
