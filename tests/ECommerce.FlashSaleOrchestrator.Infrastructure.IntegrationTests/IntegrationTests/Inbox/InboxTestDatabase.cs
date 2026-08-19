using ECommerce.FlashSaleOrchestrator.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.FlashSaleOrchestrator.Infrastructure.IntegrationTests.Inbox;

internal sealed class InboxTestDatabase
    : IAsyncDisposable
{
    private readonly string _connectionString;

    private InboxTestDatabase(
        string connectionString)
    {
        _connectionString =
            connectionString;
    }

    public string ConnectionString =>
        _connectionString;

    public static async Task<InboxTestDatabase> CreateAsync()
    {
        var baseConnectionString =
            Environment.GetEnvironmentVariable(
                "FLASHSALE_SQL_CONNECTION");

        if (string.IsNullOrWhiteSpace(
            baseConnectionString))
        {
            throw new InvalidOperationException(
                "Environment variable 'FLASHSALE_SQL_CONNECTION' must be configured.");
        }

        var connectionStringBuilder =
            new SqlConnectionStringBuilder(
                baseConnectionString)
            {
                InitialCatalog =
                    $"FlashSaleInboxTests_{Guid.NewGuid():N}"
            };

        var database =
            new InboxTestDatabase(
                connectionStringBuilder.ConnectionString);

        await using var context =
            database.CreateContext();

        await context.Database.MigrateAsync();

        return database;
    }

    public FlashSaleOrchestratorDbContext CreateContext()
    {
        var options =
            new DbContextOptionsBuilder<
                FlashSaleOrchestratorDbContext>()
                .UseSqlServer(
                    _connectionString)
                .Options;

        return new FlashSaleOrchestratorDbContext(
            options);
    }

    public async ValueTask DisposeAsync()
    {
        await using var context =
            CreateContext();

        await context.Database.EnsureDeletedAsync();
    }
}