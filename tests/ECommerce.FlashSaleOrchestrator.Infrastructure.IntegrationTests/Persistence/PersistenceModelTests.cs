using ECommerce.FlashSaleOrchestrator.Domain.Carts;
using ECommerce.FlashSaleOrchestrator.Domain.FlashSales;
using ECommerce.FlashSaleOrchestrator.Domain.Inventory;
using ECommerce.FlashSaleOrchestrator.Domain.Products;
using ECommerce.FlashSaleOrchestrator.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using ECommerce.FlashSaleOrchestrator.Infrastructure.Persistence.AlternativeRecommendations;

namespace ECommerce.FlashSaleOrchestrator.Infrastructure.IntegrationTests.Persistence;

public sealed class PersistenceModelTests
{
    [Fact]
    public void Model_ShouldContainExpectedAggregateMappings()
    {
        var options =
            new DbContextOptionsBuilder<
                FlashSaleOrchestratorDbContext>()
                .UseSqlServer(
                    "Server=localhost;" +
                    "Database=ModelValidation;" +
                    "Trusted_Connection=True;" +
                    "TrustServerCertificate=True;")
                .Options;

        using var dbContext =
            new FlashSaleOrchestratorDbContext(
                options);

        var model =
            dbContext.Model;

        Assert.NotNull(
            model.FindEntityType(
                typeof(Product)));

        Assert.NotNull(
            model.FindEntityType(
                typeof(InventoryItem)));

        Assert.NotNull(
            model.FindEntityType(
                typeof(Cart)));

        Assert.NotNull(
            model.FindEntityType(
                typeof(FlashSale)));
    }

    [Fact]
    public void InventoryItem_ShouldUseRowVersionConcurrencyToken()
    {
        var options =
            new DbContextOptionsBuilder<
                FlashSaleOrchestratorDbContext>()
                .UseSqlServer(
                    "Server=localhost;" +
                    "Database=ModelValidation;" +
                    "Trusted_Connection=True;" +
                    "TrustServerCertificate=True;")
                .Options;

        using var dbContext =
            new FlashSaleOrchestratorDbContext(
                options);

        var inventoryEntity =
            dbContext.Model.FindEntityType(
                typeof(InventoryItem));

        Assert.NotNull(
            inventoryEntity);

        var rowVersion =
            inventoryEntity.FindProperty(
                "RowVersion");

        Assert.NotNull(
            rowVersion);

        Assert.True(
            rowVersion.IsConcurrencyToken);
    }

    [Fact]
    public void Cart_ShouldOwnCartItems()
    {
        var options =
            new DbContextOptionsBuilder<
                FlashSaleOrchestratorDbContext>()
                .UseSqlServer(
                    "Server=localhost;" +
                    "Database=ModelValidation;" +
                    "Trusted_Connection=True;" +
                    "TrustServerCertificate=True;")
                .Options;

        using var dbContext =
            new FlashSaleOrchestratorDbContext(
                options);

        var cartEntity =
            dbContext.Model.FindEntityType(
                typeof(Cart));

        Assert.NotNull(
            cartEntity);

        var itemsNavigation =
            cartEntity.FindNavigation(
                nameof(Cart.Items));

        Assert.NotNull(
            itemsNavigation);

        Assert.True(
            itemsNavigation.TargetEntityType.IsOwned());
    }

    [Fact]
    public void FlashSale_ShouldOwnEligibleProducts()
    {
        var options =
            new DbContextOptionsBuilder<
                FlashSaleOrchestratorDbContext>()
                .UseSqlServer(
                    "Server=localhost;" +
                    "Database=ModelValidation;" +
                    "Trusted_Connection=True;" +
                    "TrustServerCertificate=True;")
                .Options;

        using var dbContext =
            new FlashSaleOrchestratorDbContext(
                options);

        var flashSaleEntity =
            dbContext.Model.FindEntityType(
                typeof(FlashSale));

        Assert.NotNull(
            flashSaleEntity);

        var eligibilityNavigation =
            flashSaleEntity.FindNavigation(
                nameof(
                    FlashSale.EligibleProducts));

        Assert.NotNull(
            eligibilityNavigation);

        Assert.True(
            eligibilityNavigation
                .TargetEntityType
                .IsOwned());
    }

    [Fact]
    public void Model_ShouldContainAlternativeRecommendationPlanMapping()
    {
        var options =
            new DbContextOptionsBuilder<
                FlashSaleOrchestratorDbContext>()
                .UseSqlServer(
                    "Server=localhost;" +
                    "Database=ModelValidation;" +
                    "Trusted_Connection=True;" +
                    "TrustServerCertificate=True;")
                .Options;

        using var dbContext =
            new FlashSaleOrchestratorDbContext(
                options);

        var model =
            dbContext.Model;

        var planEntity =
            model.FindEntityType(
                typeof(
                    AlternativeRecommendationPlanRecord));

        Assert.NotNull(
            planEntity);

        Assert.Equal(
            "AlternativeRecommendationPlans",
            planEntity.GetTableName());
    }

    [Fact]
    public void AlternativeRecommendationPlan_ShouldUseEventIdAsPrimaryKey()
    {
        var options =
            new DbContextOptionsBuilder<
                FlashSaleOrchestratorDbContext>()
                .UseSqlServer(
                    "Server=localhost;" +
                    "Database=ModelValidation;" +
                    "Trusted_Connection=True;" +
                    "TrustServerCertificate=True;")
                .Options;

        using var dbContext =
            new FlashSaleOrchestratorDbContext(
                options);

        var planEntity =
            dbContext.Model.FindEntityType(
                typeof(
                    AlternativeRecommendationPlanRecord));

        Assert.NotNull(
            planEntity);

        var primaryKey =
            planEntity.FindPrimaryKey();

        Assert.NotNull(
            primaryKey);

        var primaryKeyProperty =
            Assert.Single(
                primaryKey.Properties);

        Assert.Equal(
            nameof(
                AlternativeRecommendationPlanRecord.EventId),
            primaryKeyProperty.Name);

        Assert.Equal(
            ValueGenerated.Never,
            primaryKeyProperty.ValueGenerated);
    }

    [Fact]
    public void AlternativeRecommendationPlan_ShouldConfigurePersistenceColumns()
    {
        var options =
            new DbContextOptionsBuilder<
                FlashSaleOrchestratorDbContext>()
                .UseSqlServer(
                    "Server=localhost;" +
                    "Database=ModelValidation;" +
                    "Trusted_Connection=True;" +
                    "TrustServerCertificate=True;")
                .Options;

        using var dbContext =
            new FlashSaleOrchestratorDbContext(
                options);

        var planEntity =
            dbContext.Model.FindEntityType(
                typeof(
                    AlternativeRecommendationPlanRecord));

        Assert.NotNull(
            planEntity);

        var payloadProperty =
            planEntity.FindProperty(
                nameof(
                    AlternativeRecommendationPlanRecord.PayloadJson));

        Assert.NotNull(
            payloadProperty);

        Assert.False(
            payloadProperty.IsNullable);

        Assert.Equal(
            "nvarchar(max)",
            payloadProperty.GetColumnType());

        var sourceProperty =
            planEntity.FindProperty(
                nameof(
                    AlternativeRecommendationPlanRecord.Source));

        Assert.NotNull(
            sourceProperty);

        Assert.False(
            sourceProperty.IsNullable);

        Assert.Equal(
            32,
            sourceProperty.GetMaxLength());

        var sourceConverter =
            sourceProperty
                .GetTypeMapping()
                .Converter;

        Assert.NotNull(
            sourceConverter);

        Assert.Equal(
            typeof(string),
            sourceConverter.ProviderClrType);

        var correlationIdProperty =
            planEntity.FindProperty(
                nameof(
                    AlternativeRecommendationPlanRecord.CorrelationId));

        Assert.NotNull(
            correlationIdProperty);

        Assert.False(
            correlationIdProperty.IsNullable);

        Assert.Equal(
            128,
            correlationIdProperty.GetMaxLength());

        var createdAtUtcProperty =
            planEntity.FindProperty(
                nameof(
                    AlternativeRecommendationPlanRecord.CreatedAtUtc));

        Assert.NotNull(
            createdAtUtcProperty);

        Assert.False(
            createdAtUtcProperty.IsNullable);
    }
}