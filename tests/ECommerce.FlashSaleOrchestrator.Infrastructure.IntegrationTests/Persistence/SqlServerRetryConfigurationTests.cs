using ECommerce.FlashSaleOrchestrator.Infrastructure
    .Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.FlashSaleOrchestrator
    .Infrastructure.IntegrationTests.Persistence;

public sealed class SqlServerRetryConfigurationTests
{
    private const string ConnectionString =
        "Server=localhost,1433;" +
        "Database=FlashSaleOrchestrator;" +
        "User Id=sa;" +
        "Password=integration-test-password;" +
        "TrustServerCertificate=True;";

    [Fact]
    public void
        AddInfrastructure_ShouldEnableExecutionStrategyRetry_WhenRequested()
    {
        var services =
            new ServiceCollection();

        services.AddInfrastructure(
            ConnectionString,
            enableSqlRetryOnFailure:
                true);

        using var serviceProvider =
            services.BuildServiceProvider();

        using var scope =
            serviceProvider.CreateScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<
                    FlashSaleOrchestratorDbContext>();

        var executionStrategy =
            dbContext.Database
                .CreateExecutionStrategy();

        Assert.True(
            executionStrategy.RetriesOnFailure);
    }

    [Fact]
    public void
        AddInfrastructure_ShouldKeepExecutionStrategyRetryDisabled_ByDefault()
    {
        var services =
            new ServiceCollection();

        services.AddInfrastructure(
            ConnectionString);

        using var serviceProvider =
            services.BuildServiceProvider();

        using var scope =
            serviceProvider.CreateScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<
                    FlashSaleOrchestratorDbContext>();

        var executionStrategy =
            dbContext.Database
                .CreateExecutionStrategy();

        Assert.False(
            executionStrategy.RetriesOnFailure);
    }
}