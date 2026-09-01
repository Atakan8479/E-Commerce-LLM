using System.Text.Json;
using ECommerce.FlashSaleOrchestrator.Domain.Carts;
using ECommerce.FlashSaleOrchestrator.Domain.Inventory;
using ECommerce.FlashSaleOrchestrator.Domain.Inventory.Events;
using ECommerce.FlashSaleOrchestrator.Domain.Products;
using ECommerce.FlashSaleOrchestrator.Infrastructure.AlternativeCandidates;
using ECommerce.FlashSaleOrchestrator.Infrastructure.Persistence;
using ECommerce.FlashSaleOrchestrator.Infrastructure.Persistence.Repositories;
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
    public async Task AlternativeCandidateProvider_ShouldFilterByCategoryOrderAndLimitCandidates()
    {
        await using var database =
            await TestDatabase.CreateAsync();

        var excludedProductId =
            ProductId.New();

        var highStockProductId =
            ProductId.New();

        var mediumStockProductId =
            ProductId.New();

        var lowStockProductId =
            ProductId.New();

        var unavailableProductId =
            ProductId.New();

        var differentCategoryProductId =
            ProductId.New();

        var mouseCategory =
            ProductCategory.From(
                "Mouse");

        var keyboardCategory =
            ProductCategory.From(
                "Keyboard");

        await using (var arrangeContext =
            database.CreateContext())
        {
            arrangeContext.Products.AddRange(
                Product.Create(
                    excludedProductId,
                    ProductName.From(
                        "Excluded Mouse"),
                    mouseCategory),
                Product.Create(
                    highStockProductId,
                    ProductName.From(
                        "High Stock Mouse"),
                    mouseCategory),
                Product.Create(
                    mediumStockProductId,
                    ProductName.From(
                        "Medium Stock Mouse"),
                    mouseCategory),
                Product.Create(
                    lowStockProductId,
                    ProductName.From(
                        "Low Stock Mouse"),
                    mouseCategory),
                Product.Create(
                    unavailableProductId,
                    ProductName.From(
                        "Unavailable Mouse"),
                    mouseCategory),
                Product.Create(
                    differentCategoryProductId,
                    ProductName.From(
                        "Mechanical Keyboard"),
                    keyboardCategory));

            arrangeContext.InventoryItems.AddRange(
                InventoryItem.Create(
                    excludedProductId,
                    StockQuantity.From(100)),
                InventoryItem.Create(
                    highStockProductId,
                    StockQuantity.From(20)),
                InventoryItem.Create(
                    mediumStockProductId,
                    StockQuantity.From(12)),
                InventoryItem.Create(
                    lowStockProductId,
                    StockQuantity.From(5)),
                InventoryItem.Create(
                    unavailableProductId,
                    StockQuantity.Zero),
                InventoryItem.Create(
                    differentCategoryProductId,
                    StockQuantity.From(100)));

            await arrangeContext.SaveChangesAsync();
        }

        await using var queryContext =
            database.CreateContext();

        var provider =
            new SqlAlternativeCandidateProvider(
                queryContext);

        var candidateSet =
            await provider.GetCandidateSetAsync(
                excludedProductId.Value,
                10);

        Assert.NotNull(
            candidateSet);

        Assert.Equal(
            excludedProductId.Value,
            candidateSet.DepletedProduct.ProductId);

        Assert.Equal(
            "Excluded Mouse",
            candidateSet.DepletedProduct.Name);

        Assert.Equal(
            "mouse",
            candidateSet.DepletedProduct.Category);

        var allCandidates =
            candidateSet.Candidates;

        Assert.Equal(
            3,
            allCandidates.Count);

        Assert.Collection(
            allCandidates,
            candidate =>
            {
                Assert.Equal(
                    highStockProductId.Value,
                    candidate.ProductId);

                Assert.Equal(
                    "High Stock Mouse",
                    candidate.Name);

                Assert.Equal(
                    "mouse",
                    candidate.Category);

                Assert.Equal(
                    20,
                    candidate.AvailableQuantity);
            },
            candidate =>
            {
                Assert.Equal(
                    mediumStockProductId.Value,
                    candidate.ProductId);

                Assert.Equal(
                    "Medium Stock Mouse",
                    candidate.Name);

                Assert.Equal(
                    "mouse",
                    candidate.Category);

                Assert.Equal(
                    12,
                    candidate.AvailableQuantity);
            },
            candidate =>
            {
                Assert.Equal(
                    lowStockProductId.Value,
                    candidate.ProductId);

                Assert.Equal(
                    "Low Stock Mouse",
                    candidate.Name);

                Assert.Equal(
                    "mouse",
                    candidate.Category);

                Assert.Equal(
                    5,
                    candidate.AvailableQuantity);
            });

        Assert.DoesNotContain(
            allCandidates,
            candidate =>
                candidate.ProductId ==
                excludedProductId.Value);

        Assert.DoesNotContain(
            allCandidates,
            candidate =>
                candidate.ProductId ==
                unavailableProductId.Value);

        Assert.DoesNotContain(
            allCandidates,
            candidate =>
                candidate.ProductId ==
                differentCategoryProductId.Value);

        var limitedCandidateSet =
            await provider.GetCandidateSetAsync(
                excludedProductId.Value,
                2);

        Assert.NotNull(
            limitedCandidateSet);

        var limitedCandidates =
            limitedCandidateSet.Candidates;

        Assert.Equal(
            2,
            limitedCandidates.Count);

        Assert.Equal(
            highStockProductId.Value,
            limitedCandidates[0].ProductId);

        Assert.Equal(
            mediumStockProductId.Value,
            limitedCandidates[1].ProductId);
    }

    [Fact]
    public async Task AlternativeCandidateProvider_ShouldReturnEmpty_WhenDepletedProductIsUncategorized()
    {
        await using var database =
            await TestDatabase.CreateAsync();

        var depletedProductId =
            ProductId.New();

        var candidateProductId =
            ProductId.New();

        await using (var arrangeContext =
            database.CreateContext())
        {
            arrangeContext.Products.AddRange(
                Product.Create(
                    depletedProductId,
                    ProductName.From(
                        "Uncategorized Depleted Product")),
                Product.Create(
                    candidateProductId,
                    ProductName.From(
                        "Uncategorized Candidate Product")));

            arrangeContext.InventoryItems.AddRange(
                InventoryItem.Create(
                    depletedProductId,
                    StockQuantity.Zero),
                InventoryItem.Create(
                    candidateProductId,
                    StockQuantity.From(100)));

            await arrangeContext.SaveChangesAsync();
        }

        await using var queryContext =
            database.CreateContext();

        var provider =
            new SqlAlternativeCandidateProvider(
                queryContext);

        var candidateSet =
            await provider.GetCandidateSetAsync(
                depletedProductId.Value,
                10);

        Assert.NotNull(
            candidateSet);

        Assert.Equal(
            depletedProductId.Value,
            candidateSet.DepletedProduct.ProductId);

        Assert.Equal(
            "Uncategorized Depleted Product",
            candidateSet.DepletedProduct.Name);

        Assert.Empty(
            candidateSet.Candidates);
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
                Cart.Create(
                    cartId);

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

        firstInventoryItem.DecreaseStock(
            1);

        await firstContext.SaveChangesAsync();

        secondInventoryItem.DecreaseStock(
            1);

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

    [Fact]
    public async Task AlternativeCandidateProvider_ShouldReturnNull_WhenDepletedProductDoesNotExist()
    {
        await using var database =
            await TestDatabase.CreateAsync();

        var candidateProductId =
            ProductId.New();

        await using (var arrangeContext =
            database.CreateContext())
        {
            arrangeContext.Products.Add(
                Product.Create(
                    candidateProductId,
                    ProductName.From(
                        "Existing Mouse"),
                    ProductCategory.From(
                        "Mouse")));

            arrangeContext.InventoryItems.Add(
                InventoryItem.Create(
                    candidateProductId,
                    StockQuantity.From(50)));

            await arrangeContext.SaveChangesAsync();
        }

        await using var queryContext =
            database.CreateContext();

        var provider =
            new SqlAlternativeCandidateProvider(
                queryContext);

        var candidateSet =
            await provider.GetCandidateSetAsync(
                Guid.NewGuid(),
                10);

        Assert.Null(
            candidateSet);
    }

    [Fact]
    public async Task AlternativeCandidateProvider_ShouldThrow_WhenDepletedProductIdIsEmpty()
    {
        await using var database =
            await TestDatabase.CreateAsync();

        await using var queryContext =
            database.CreateContext();

        var provider =
            new SqlAlternativeCandidateProvider(
                queryContext);

        var exception =
            await Assert.ThrowsAsync<ArgumentException>(
                () =>
                    provider.GetCandidateSetAsync(
                        Guid.Empty,
                        10));

        Assert.Equal(
            "depletedProductId",
            exception.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10)]
    public async Task AlternativeCandidateProvider_ShouldThrow_WhenLimitIsNotPositive(
        int limit)
    {
        await using var database =
            await TestDatabase.CreateAsync();

        await using var queryContext =
            database.CreateContext();

        var provider =
            new SqlAlternativeCandidateProvider(
                queryContext);

        var exception =
            await Assert.ThrowsAsync<
                ArgumentOutOfRangeException>(
                () =>
                    provider.GetCandidateSetAsync(
                        Guid.NewGuid(),
                        limit));

        Assert.Equal(
            "limit",
            exception.ParamName);
    }

    [Fact]
    public async Task AlternativeCandidateProvider_ShouldReturnSameOrder_WhenCandidateStockIsEqual()
    {
        await using var database =
            await TestDatabase.CreateAsync();

        var depletedProductId =
            ProductId.New();

        var firstCandidateId =
            ProductId.New();

        var secondCandidateId =
            ProductId.New();

        var thirdCandidateId =
            ProductId.New();

        var mouseCategory =
            ProductCategory.From(
                "Mouse");

        await using (var arrangeContext =
            database.CreateContext())
        {
            arrangeContext.Products.AddRange(
                Product.Create(
                    depletedProductId,
                    ProductName.From(
                        "Depleted Mouse"),
                    mouseCategory),
                Product.Create(
                    firstCandidateId,
                    ProductName.From(
                        "Mouse A"),
                    mouseCategory),
                Product.Create(
                    secondCandidateId,
                    ProductName.From(
                        "Mouse B"),
                    mouseCategory),
                Product.Create(
                    thirdCandidateId,
                    ProductName.From(
                        "Mouse C"),
                    mouseCategory));

            arrangeContext.InventoryItems.AddRange(
                InventoryItem.Create(
                    depletedProductId,
                    StockQuantity.Zero),
                InventoryItem.Create(
                    firstCandidateId,
                    StockQuantity.From(10)),
                InventoryItem.Create(
                    secondCandidateId,
                    StockQuantity.From(10)),
                InventoryItem.Create(
                    thirdCandidateId,
                    StockQuantity.From(10)));

            await arrangeContext.SaveChangesAsync();
        }

        await using var queryContext =
            database.CreateContext();

        var provider =
            new SqlAlternativeCandidateProvider(
                queryContext);

        var firstCandidateSet =
            await provider.GetCandidateSetAsync(
                depletedProductId.Value,
                2);

        var secondCandidateSet =
            await provider.GetCandidateSetAsync(
                depletedProductId.Value,
                2);

        Assert.NotNull(
            firstCandidateSet);

        Assert.NotNull(
            secondCandidateSet);

        var firstResult =
            firstCandidateSet.Candidates;

        var secondResult =
            secondCandidateSet.Candidates;

        Assert.Equal(
            2,
            firstResult.Count);

        Assert.Equal(
            firstResult
                .Select(
                    candidate =>
                        candidate.ProductId),
            secondResult
                .Select(
                    candidate =>
                        candidate.ProductId));

        Assert.All(
            firstResult,
            candidate =>
                Assert.Equal(
                    10,
                    candidate.AvailableQuantity));
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
}