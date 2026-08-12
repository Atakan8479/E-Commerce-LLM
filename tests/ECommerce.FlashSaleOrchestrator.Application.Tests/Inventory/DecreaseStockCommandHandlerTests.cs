using ECommerce.FlashSaleOrchestrator.Application.Abstractions.Persistence;
using ECommerce.FlashSaleOrchestrator.Application.Inventory.DecreaseStock;
using ECommerce.FlashSaleOrchestrator.Domain.Inventory;
using ECommerce.FlashSaleOrchestrator.Domain.Products;

namespace ECommerce.FlashSaleOrchestrator.Application.Tests.Inventory;

public sealed class DecreaseStockCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldDecreaseStockAndPersistChanges()
    {
        var productId =
            ProductId.New();

        var inventoryItem =
            InventoryItem.Create(
                productId,
                StockQuantity.From(5));

        var repository =
            new FakeInventoryRepository(
                inventoryItem);

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            new DecreaseStockCommandHandler(
                repository,
                unitOfWork);

        var result =
            await handler.HandleAsync(
                new DecreaseStockCommand(
                    productId.Value,
                    2));

        Assert.Equal(
            productId.Value,
            result.ProductId);

        Assert.Equal(
            3,
            result.RemainingQuantity);

        Assert.False(
            result.IsDepleted);

        Assert.Equal(
            3,
            inventoryItem.AvailableQuantity.Value);

        Assert.Equal(
            1,
            unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnDepletedResult_WhenStockTransitionsToZero()
    {
        var productId =
            ProductId.New();

        var inventoryItem =
            InventoryItem.Create(
                productId,
                StockQuantity.From(2));

        var repository =
            new FakeInventoryRepository(
                inventoryItem);

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            new DecreaseStockCommandHandler(
                repository,
                unitOfWork);

        var result =
            await handler.HandleAsync(
                new DecreaseStockCommand(
                    productId.Value,
                    2));

        Assert.Equal(
            0,
            result.RemainingQuantity);

        Assert.True(
            result.IsDepleted);

        Assert.Equal(
            1,
            unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleAsync_ShouldThrowInventoryItemNotFoundException_WhenInventoryDoesNotExist()
    {
        var productId =
            Guid.NewGuid();

        var repository =
            new FakeInventoryRepository(
                null);

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            new DecreaseStockCommandHandler(
                repository,
                unitOfWork);

        var exception =
            await Assert.ThrowsAsync<InventoryItemNotFoundException>(
                () => handler.HandleAsync(
                    new DecreaseStockCommand(
                        productId,
                        1)));

        Assert.Equal(
            productId,
            exception.ProductId);

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCallCount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task HandleAsync_ShouldThrowArgumentOutOfRangeException_WhenQuantityIsNotPositive(
        int quantity)
    {
        var productId =
            ProductId.New();

        var inventoryItem =
            InventoryItem.Create(
                productId,
                StockQuantity.From(5));

        var repository =
            new FakeInventoryRepository(
                inventoryItem);

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            new DecreaseStockCommandHandler(
                repository,
                unitOfWork);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => handler.HandleAsync(
                new DecreaseStockCommand(
                    productId.Value,
                    quantity)));

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleAsync_ShouldThrowArgumentException_WhenProductIdentifierIsEmpty()
    {
        var repository =
            new FakeInventoryRepository(
                null);

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            new DecreaseStockCommandHandler(
                repository,
                unitOfWork);

        await Assert.ThrowsAsync<ArgumentException>(
            () => handler.HandleAsync(
                new DecreaseStockCommand(
                    Guid.Empty,
                    1)));

        Assert.Equal(
            0,
            repository.GetByProductIdCallCount);

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCallCount);
    }

    private sealed class FakeInventoryRepository
        : IInventoryRepository
    {
        private readonly InventoryItem? _inventoryItem;

        public int GetByProductIdCallCount { get; private set; }

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

    private sealed class FakeUnitOfWork
        : IUnitOfWork
    {
        public int SaveChangesCallCount { get; private set; }

        public Task SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            SaveChangesCallCount++;

            return Task.CompletedTask;
        }
    }
}