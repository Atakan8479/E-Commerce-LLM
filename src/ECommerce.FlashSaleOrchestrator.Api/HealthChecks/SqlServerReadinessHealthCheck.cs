using ECommerce.FlashSaleOrchestrator.Infrastructure
    .Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ECommerce.FlashSaleOrchestrator.Api
    .HealthChecks;

public sealed class SqlServerReadinessHealthCheck
    : IHealthCheck
{
    private readonly FlashSaleOrchestratorDbContext
        _dbContext;

    public SqlServerReadinessHealthCheck(
        FlashSaleOrchestratorDbContext dbContext)
    {
        _dbContext =
            dbContext
            ?? throw new ArgumentNullException(
                nameof(dbContext));
    }

    public async Task<HealthCheckResult>
        CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
    {
        try
        {
            var canConnect =
                await _dbContext.Database
                    .CanConnectAsync(
                        cancellationToken);

            return canConnect
                ? HealthCheckResult.Healthy(
                    "SQL Server is reachable.")
                : HealthCheckResult.Unhealthy(
                    "SQL Server is not reachable.");
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy(
                "SQL Server connectivity check failed.",
                exception);
        }
    }
}