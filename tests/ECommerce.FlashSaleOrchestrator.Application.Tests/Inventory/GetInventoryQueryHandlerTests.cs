using ECommerce.FlashSaleOrchestrator.Application
    .Abstractions.Persistence;
using ECommerce.FlashSaleOrchestrator.Application
    .Inventory.GetInventory;
using ECommerce.FlashSaleOrchestrator.Domain.Inventory;
using ECommerce.FlashSaleOrchestrator.Domain.Products;

namespace ECommerce.FlashSaleOrchestrator.Application.Tests
    .Inventory;

public sealed class GetInventoryQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldReturnMappedInventory_WhenInventoryExists()
    {
        var productId =
            ProductId.New();

        var inventoryItem =
            InventoryItem.Create(
                productId,
                StockQuantity.From(7));

        var repository =
            new FakeInventoryRepository(
                inventoryItem);

        var handler =
            new GetInventoryQueryHandler(
                repository);

        var result =
            await handler.HandleAsync(
                new GetInventoryQuery(
                    productId.Value));

        Assert.NotNull(
            result);

        Assert.Equal(
            productId.Value,
            result.ProductId);

        Assert.Equal(
            7,
            result.AvailableQuantity);

        Assert.False(
            result.IsDepleted);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnNull_WhenInventoryDoesNotExist()
    {
        var repository =
            new FakeInventoryRepository(
                null);

        var handler =
            new GetInventoryQueryHandler(
                repository);

        var result =
            await handler.HandleAsync(
                new GetInventoryQuery(
                    Guid.NewGuid()));

        Assert.Null(
            result);

        Assert.Equal(
            1,
            repository.GetByProductIdCallCount);
    }

    [Fact]
    public async Task HandleAsync_ShouldThrowArgumentException_WhenProductIdentifierIsEmpty()
    {
        var repository =
            new FakeInventoryRepository(
                null);

        var handler =
            new GetInventoryQueryHandler(
                repository);

        await Assert.ThrowsAsync<ArgumentException>(
            () => handler.HandleAsync(
                new GetInventoryQuery(
                    Guid.Empty)));

        Assert.Equal(
            0,
            repository.GetByProductIdCallCount);
    }

    private sealed class FakeInventoryRepository
        : IInventoryRepository
    {
        private readonly InventoryItem?
            _inventoryItem;

        public int GetByProductIdCallCount
        {
            get;
            private set;
        }

        public FakeInventoryRepository(
            InventoryItem? inventoryItem)
        {
            _inventoryItem =
                inventoryItem;
        }

        public Task<InventoryItem?> GetByProductIdAsync(
            ProductId productId,
            CancellationToken cancellationToken = default)
        {
            GetByProductIdCallCount++;

            return Task.FromResult(
                _inventoryItem);
        }
    }
}