using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ECommerce.FlashSaleOrchestrator.Infrastructure.Persistence;

public sealed class FlashSaleOrchestratorDbContextFactory
    : IDesignTimeDbContextFactory<FlashSaleOrchestratorDbContext>
{
    public FlashSaleOrchestratorDbContext CreateDbContext(
        string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable(
                "FLASHSALE_SQL_CONNECTION");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Environment variable 'FLASHSALE_SQL_CONNECTION' must be configured.");
        }

        var optionsBuilder =
            new DbContextOptionsBuilder<
                FlashSaleOrchestratorDbContext>();

        optionsBuilder.UseSqlServer(
            connectionString);

        return new FlashSaleOrchestratorDbContext(
            optionsBuilder.Options);
    }
}