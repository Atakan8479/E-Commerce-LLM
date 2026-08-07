using ECommerce.FlashSaleOrchestrator.Domain.Inventory;
using ECommerce.FlashSaleOrchestrator.Domain.Inventory.Events;
using ECommerce.FlashSaleOrchestrator.Domain.Inventory.Exceptions;
using ECommerce.FlashSaleOrchestrator.Domain.Products;

namespace ECommerce.FlashSaleOrchestrator.Domain.Tests.Inventory;

public sealed class InventoryItemTests
{
    [Fact]
    public void Create_ShouldPreserveProductAndAvailableQuantity()
    {
        var productId =
            ProductId.New();

        var availableQuantity =
            StockQuantity.From(10);

        var inventoryItem =
            InventoryItem.Create(
                productId,
                availableQuantity);

        Assert.Equal(
            productId,
            inventoryItem.ProductId);

        Assert.Equal(
            availableQuantity,
            inventoryItem.AvailableQuantity);

        Assert.False(
            inventoryItem.IsDepleted);
    }

    [Fact]
    public void Create_ShouldThrowArgumentNullException_WhenProductIdIsNull()
    {
        var availableQuantity =
            StockQuantity.From(10);

        var exception =
            Assert.Throws<ArgumentNullException>(
                () => InventoryItem.Create(
                    null!,
                    availableQuantity));

        Assert.Equal(
            "productId",
            exception.ParamName);
    }

    [Fact]
    public void Create_ShouldThrowArgumentNullException_WhenAvailableQuantityIsNull()
    {
        var exception =
            Assert.Throws<ArgumentNullException>(
                () => InventoryItem.Create(
                    ProductId.New(),
                    null!));

        Assert.Equal(
            "availableQuantity",
            exception.ParamName);
    }

    [Fact]
    public void Create_ShouldNotRaiseStockDepletedEvent_WhenInitialQuantityIsZero()
    {
        var inventoryItem =
            InventoryItem.Create(
                ProductId.New(),
                StockQuantity.Zero);

        Assert.True(
            inventoryItem.IsDepleted);

        Assert.Empty(
            inventoryItem.DomainEvents);
    }

    [Fact]
    public void IncreaseStock_ShouldIncreaseAvailableQuantity()
    {
        var inventoryItem =
            InventoryItem.Create(
                ProductId.New(),
                StockQuantity.From(5));

        inventoryItem.IncreaseStock(3);

        Assert.Equal(
            8,
            inventoryItem.AvailableQuantity.Value);

        Assert.False(
            inventoryItem.IsDepleted);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void IncreaseStock_ShouldThrowArgumentOutOfRangeException_WhenQuantityIsNotPositive(
        int quantity)
    {
        var inventoryItem =
            InventoryItem.Create(
                ProductId.New(),
                StockQuantity.From(5));

        var exception =
            Assert.Throws<ArgumentOutOfRangeException>(
                () => inventoryItem.IncreaseStock(quantity));

        Assert.Equal(
            "quantity",
            exception.ParamName);

        Assert.Equal(
            5,
            inventoryItem.AvailableQuantity.Value);
    }

    [Fact]
    public void DecreaseStock_ShouldDecreaseAvailableQuantity()
    {
        var inventoryItem =
            InventoryItem.Create(
                ProductId.New(),
                StockQuantity.From(10));

        inventoryItem.DecreaseStock(4);

        Assert.Equal(
            6,
            inventoryItem.AvailableQuantity.Value);

        Assert.False(
            inventoryItem.IsDepleted);
    }

    [Fact]
    public void DecreaseStock_ShouldRaiseStockDepletedEvent_WhenQuantityTransitionsToZero()
    {
        var productId =
            ProductId.New();

        var inventoryItem =
            InventoryItem.Create(
                productId,
                StockQuantity.From(2));

        inventoryItem.DecreaseStock(2);

        Assert.Equal(
            0,
            inventoryItem.AvailableQuantity.Value);

        Assert.True(
            inventoryItem.IsDepleted);

        var domainEvent =
            Assert.Single(
                inventoryItem.DomainEvents);

        var stockDepletedEvent =
            Assert.IsType<StockDepletedDomainEvent>(
                domainEvent);

        Assert.Equal(
            productId,
            stockDepletedEvent.ProductId);
    }

    [Fact]
    public void DecreaseStock_ShouldNotRaiseStockDepletedEvent_WhenStockRemainsAvailable()
    {
        var inventoryItem =
            InventoryItem.Create(
                ProductId.New(),
                StockQuantity.From(10));

        inventoryItem.DecreaseStock(3);

        Assert.Equal(
            7,
            inventoryItem.AvailableQuantity.Value);

        Assert.Empty(
            inventoryItem.DomainEvents);
    }

    [Fact]
    public void DecreaseStock_ShouldThrowInsufficientStockException_WhenRequestedQuantityExceedsAvailableStock()
    {
        var productId =
            ProductId.New();

        var inventoryItem =
            InventoryItem.Create(
                productId,
                StockQuantity.From(2));

        var exception =
            Assert.Throws<InsufficientStockException>(
                () => inventoryItem.DecreaseStock(3));

        Assert.Equal(
            productId,
            exception.ProductId);

        Assert.Equal(
            2,
            exception.AvailableQuantity);

        Assert.Equal(
            3,
            exception.RequestedQuantity);

        Assert.Equal(
            2,
            inventoryItem.AvailableQuantity.Value);

        Assert.Empty(
            inventoryItem.DomainEvents);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void DecreaseStock_ShouldThrowArgumentOutOfRangeException_WhenQuantityIsNotPositive(
        int quantity)
    {
        var inventoryItem =
            InventoryItem.Create(
                ProductId.New(),
                StockQuantity.From(5));

        var exception =
            Assert.Throws<ArgumentOutOfRangeException>(
                () => inventoryItem.DecreaseStock(quantity));

        Assert.Equal(
            "quantity",
            exception.ParamName);

        Assert.Equal(
            5,
            inventoryItem.AvailableQuantity.Value);
    }

    [Fact]
    public void DecreaseStock_ShouldThrowInsufficientStockException_WhenStockIsAlreadyDepleted()
    {
        var inventoryItem =
            InventoryItem.Create(
                ProductId.New(),
                StockQuantity.Zero);

        Assert.Throws<InsufficientStockException>(
            () => inventoryItem.DecreaseStock(1));

        Assert.True(
            inventoryItem.IsDepleted);

        Assert.Empty(
            inventoryItem.DomainEvents);
    }

    [Fact]
    public void ClearDomainEvents_ShouldRemovePreviouslyRaisedEvents()
    {
        var inventoryItem =
            InventoryItem.Create(
                ProductId.New(),
                StockQuantity.From(1));

        inventoryItem.DecreaseStock(1);

        Assert.Single(
            inventoryItem.DomainEvents);

        inventoryItem.ClearDomainEvents();

        Assert.Empty(
            inventoryItem.DomainEvents);
    }

    [Fact]
    public void InventoryItem_ShouldRaiseNewStockDepletedEvent_AfterReplenishmentAndSecondDepletion()
    {
        var inventoryItem =
            InventoryItem.Create(
                ProductId.New(),
                StockQuantity.From(1));

        inventoryItem.DecreaseStock(1);

        Assert.Single(
            inventoryItem.DomainEvents);

        inventoryItem.ClearDomainEvents();

        inventoryItem.IncreaseStock(2);

        Assert.False(
            inventoryItem.IsDepleted);

        inventoryItem.DecreaseStock(2);

        Assert.True(
            inventoryItem.IsDepleted);

        Assert.Single(
            inventoryItem.DomainEvents);
    }
}