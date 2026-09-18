using ECommerce.FlashSaleOrchestrator.Application
    .Abstractions.Persistence;
using ECommerce.FlashSaleOrchestrator.Application
    .Products.GetProduct;
using ECommerce.FlashSaleOrchestrator.Domain.Products;

namespace ECommerce.FlashSaleOrchestrator.Application.Tests
    .Products;

public sealed class GetProductQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldReturnMappedProduct_WhenProductExists()
    {
        var productId =
            ProductId.New();

        var product =
            Product.Create(
                productId,
                ProductName.From(
                    "Gaming Mouse"),
                ProductCategory.From(
                    "Gaming"));

        var repository =
            new FakeProductRepository(
                product);

        var handler =
            new GetProductQueryHandler(
                repository);

        var result =
            await handler.HandleAsync(
                new GetProductQuery(
                    productId.Value));

        Assert.NotNull(
            result);

        Assert.Equal(
            productId.Value,
            result.ProductId);

        Assert.Equal(
            "Gaming Mouse",
            result.Name);

        Assert.Equal(
            "gaming",
            result.Category);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnNull_WhenProductDoesNotExist()
    {
        var repository =
            new FakeProductRepository(
                null);

        var handler =
            new GetProductQueryHandler(
                repository);

        var result =
            await handler.HandleAsync(
                new GetProductQuery(
                    Guid.NewGuid()));

        Assert.Null(
            result);

        Assert.Equal(
            1,
            repository.GetByIdCallCount);
    }

    [Fact]
    public async Task HandleAsync_ShouldThrowArgumentException_WhenProductIdentifierIsEmpty()
    {
        var repository =
            new FakeProductRepository(
                null);

        var handler =
            new GetProductQueryHandler(
                repository);

        await Assert.ThrowsAsync<ArgumentException>(
            () => handler.HandleAsync(
                new GetProductQuery(
                    Guid.Empty)));

        Assert.Equal(
            0,
            repository.GetByIdCallCount);
    }

    private sealed class FakeProductRepository
        : IProductRepository
    {
        private readonly Product?
            _product;

        public int GetByIdCallCount
        {
            get;
            private set;
        }

        public FakeProductRepository(
            Product? product)
        {
            _product =
                product;
        }

        public Task<Product?> GetByIdAsync(
            ProductId productId,
            CancellationToken cancellationToken = default)
        {
            GetByIdCallCount++;

            return Task.FromResult(
                _product);
        }
    }
}