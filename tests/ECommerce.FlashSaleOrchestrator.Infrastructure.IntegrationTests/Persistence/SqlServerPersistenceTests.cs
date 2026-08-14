using ECommerce.FlashSaleOrchestrator.Domain.Carts;
using ECommerce.FlashSaleOrchestrator.Domain.Inventory;
using ECommerce.FlashSaleOrchestrator.Domain.Products;
using ECommerce.FlashSaleOrchestrator.Infrastructure.Persistence;
using ECommerce.FlashSaleOrchestrator.Infrastructure.Persistence.Repositories;
using System.Text.Json;
using ECommerce.FlashSaleOrchestrator.Domain.Inventory.Events;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.FlashSaleOrchestrator.Infrastructure.IntegrationTests.Persistence;

public sealed class SqlServerPersistenceTests
{
    [Fact]
    public async Task InventoryRepository_ShouldLoadPersistedInventoryItem()
    {
        await using var database =
            await TestDatabase.CreateAsync();

        var productId =
            ProductId.New();

        await using (var arrangeContext =
            database.CreateContext())
        {
            arrangeContext.Products.Add(
                Product.Create(
                    productId,
                    ProductName.From(
                        "Inventory Test Product")));

            await arrangeContext.SaveChangesAsync();

            arrangeContext.InventoryItems.Add(
                InventoryItem.Create(
                    productId,
                    StockQuantity.From(5)));

            await arrangeContext.SaveChangesAsync();
        }

        await using var queryContext =
            database.CreateContext();

        var repository =
            new InventoryRepository(
                queryContext);

        var inventoryItem =
            await repository.GetByProductIdAsync(
                productId);

        Assert.NotNull(
            inventoryItem);

        Assert.Equal(
            productId,
            inventoryItem.ProductId);

        Assert.Equal(
            5,
            inventoryItem.AvailableQuantity.Value);
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldPersistOutboxMessage_WhenStockBecomesDepleted()
    {
        await using var database =
            await TestDatabase.CreateAsync();

        var productId =
            ProductId.New();

        await using (var arrangeContext =
            database.CreateContext())
        {
            arrangeContext.Products.Add(
                Product.Create(
                    productId,
                    ProductName.From(
                        "Outbox Test Product")));

            arrangeContext.InventoryItems.Add(
                InventoryItem.Create(
                    productId,
                    StockQuantity.From(1)));

            await arrangeContext.SaveChangesAsync();
        }

        await using (var actContext =
            database.CreateContext())
        {
            var inventoryItem =
                await actContext.InventoryItems.SingleAsync(
                    item =>
                        item.ProductId == productId);

            inventoryItem.DecreaseStock(1);

            Assert.Single(
                inventoryItem.DomainEvents);

            await actContext.SaveChangesAsync();

            Assert.Empty(
                inventoryItem.DomainEvents);
        }

        await using var assertContext =
            database.CreateContext();

        var persistedInventoryItem =
            await assertContext.InventoryItems
                .AsNoTracking()
                .SingleAsync(
                    item =>
                        item.ProductId == productId);

        var outboxMessage =
            await assertContext.OutboxMessages
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            0,
            persistedInventoryItem.AvailableQuantity.Value);

        Assert.NotEqual(
            Guid.Empty,
            outboxMessage.Id);

        Assert.Equal(
            typeof(StockDepletedDomainEvent).FullName,
            outboxMessage.Type);

        Assert.Null(
            outboxMessage.ProcessedAtUtc);

        Assert.NotEqual(
            default,
            outboxMessage.OccurredAtUtc);

        using var payload =
            JsonDocument.Parse(
                outboxMessage.Payload);

        var persistedProductId =
            payload.RootElement
                .GetProperty("ProductId")
                .GetProperty("Value")
                .GetGuid();

        Assert.Equal(
            productId.Value,
            persistedProductId);
    }

    [Fact]
    public async Task CartRepository_ShouldLoadCartWithItems()
    {
        await using var database =
            await TestDatabase.CreateAsync();

        var cartId =
            CartId.New();

        var firstProductId =
            ProductId.New();

        var secondProductId =
            ProductId.New();

        await using (var arrangeContext =
            database.CreateContext())
        {
            arrangeContext.Products.AddRange(
                Product.Create(
                    firstProductId,
                    ProductName.From(
                        "First Cart Product")),
                Product.Create(
                    secondProductId,
                    ProductName.From(
                        "Second Cart Product")));

            await arrangeContext.SaveChangesAsync();

            var cart =
                Cart.Create(cartId);

            cart.AddItem(
                firstProductId,
                2);

            cart.AddItem(
                secondProductId,
                3);

            arrangeContext.Carts.Add(
                cart);

            await arrangeContext.SaveChangesAsync();
        }

        await using var queryContext =
            database.CreateContext();

        var repository =
            new CartRepository(
                queryContext);

        var cartResult =
            await repository.GetByIdAsync(
                cartId);

        Assert.NotNull(
            cartResult);

        Assert.Equal(
            2,
            cartResult.Items.Count);

        Assert.Contains(
            cartResult.Items,
            item =>
                item.ProductId == firstProductId &&
                item.Quantity == 2);

        Assert.Contains(
            cartResult.Items,
            item =>
                item.ProductId == secondProductId &&
                item.Quantity == 3);
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldThrowDbUpdateConcurrencyException_WhenInventoryWasChangedByAnotherContext()
    {
        await using var database =
            await TestDatabase.CreateAsync();

        var productId =
            ProductId.New();

        await using (var arrangeContext =
            database.CreateContext())
        {
            arrangeContext.Products.Add(
                Product.Create(
                    productId,
                    ProductName.From(
                        "Concurrency Test Product")));

            await arrangeContext.SaveChangesAsync();

            arrangeContext.InventoryItems.Add(
                InventoryItem.Create(
                    productId,
                    StockQuantity.From(1)));

            await arrangeContext.SaveChangesAsync();
        }

        await using var firstContext =
            database.CreateContext();

        await using var secondContext =
            database.CreateContext();

        var firstInventoryItem =
            await firstContext.InventoryItems.SingleAsync(
                item =>
                    item.ProductId == productId);

        var secondInventoryItem =
            await secondContext.InventoryItems.SingleAsync(
                item =>
                    item.ProductId == productId);

        firstInventoryItem.DecreaseStock(1);

        await firstContext.SaveChangesAsync();

        secondInventoryItem.DecreaseStock(1);

        await Assert.ThrowsAsync<
            DbUpdateConcurrencyException>(
                () =>
                    secondContext.SaveChangesAsync());

        Assert.Single(
            secondInventoryItem.DomainEvents);

        await using var verificationContext =
            database.CreateContext();

        var persistedInventoryItem =
            await verificationContext.InventoryItems
                .AsNoTracking()
                .SingleAsync(
                    item =>
                        item.ProductId == productId);

        var persistedOutboxMessages =
            await verificationContext.OutboxMessages
                .AsNoTracking()
                .ToListAsync();

        Assert.Equal(
            0,
            persistedInventoryItem.AvailableQuantity.Value);

        Assert.Single(
            persistedOutboxMessages);

        Assert.Equal(
            typeof(StockDepletedDomainEvent).FullName,
            persistedOutboxMessages[0].Type);

        Assert.Null(
            persistedOutboxMessages[0].ProcessedAtUtc);
    }

    private sealed class TestDatabase
        : IAsyncDisposable
    {
        private readonly string _connectionString;

        private TestDatabase(
            string connectionString)
        {
            _connectionString =
                connectionString;
        }

        public static async Task<TestDatabase> CreateAsync()
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
                        $"FlashSaleTests_{Guid.NewGuid():N}"
                };

            var database =
                new TestDatabase(
                    connectionStringBuilder.ConnectionString);

            await using var context =
                database.CreateContext();

            await context.Database.MigrateAsync();

            return database;
        }

        public FlashSaleOrchestratorDbContext
            CreateContext()
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
}
