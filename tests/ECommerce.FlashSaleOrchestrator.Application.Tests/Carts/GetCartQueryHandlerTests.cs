using ECommerce.FlashSaleOrchestrator.Application.Abstractions.Persistence;
using ECommerce.FlashSaleOrchestrator.Application.Carts.GetCart;
using ECommerce.FlashSaleOrchestrator.Domain.Carts;
using ECommerce.FlashSaleOrchestrator.Domain.Products;

namespace ECommerce.FlashSaleOrchestrator.Application.Tests.Carts;

public sealed class GetCartQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldReturnMappedCart_WhenCartExists()
    {
        var cartId =
            CartId.New();

        var firstProductId =
            ProductId.New();

        var secondProductId =
            ProductId.New();

        var cart =
            Cart.Create(cartId);

        cart.AddItem(
            firstProductId,
            2);

        cart.AddItem(
            secondProductId,
            3);

        var repository =
            new FakeCartRepository(
                cart);

        var handler =
            new GetCartQueryHandler(
                repository);

        var result =
            await handler.HandleAsync(
                new GetCartQuery(
                    cartId.Value));

        Assert.NotNull(result);

        Assert.Equal(
            cartId.Value,
            result.CartId);

        Assert.Equal(
            2,
            result.Items.Count);

        Assert.Contains(
            result.Items,
            item =>
                item.ProductId == firstProductId.Value &&
                item.Quantity == 2);

        Assert.Contains(
            result.Items,
            item =>
                item.ProductId == secondProductId.Value &&
                item.Quantity == 3);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnEmptyItemCollection_WhenCartIsEmpty()
    {
        var cartId =
            CartId.New();

        var cart =
            Cart.Create(cartId);

        var repository =
            new FakeCartRepository(
                cart);

        var handler =
            new GetCartQueryHandler(
                repository);

        var result =
            await handler.HandleAsync(
                new GetCartQuery(
                    cartId.Value));

        Assert.NotNull(result);

        Assert.Empty(
            result.Items);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnNull_WhenCartDoesNotExist()
    {
        var repository =
            new FakeCartRepository(
                null);

        var handler =
            new GetCartQueryHandler(
                repository);

        var result =
            await handler.HandleAsync(
                new GetCartQuery(
                    Guid.NewGuid()));

        Assert.Null(result);
    }

    [Fact]
    public async Task HandleAsync_ShouldThrowArgumentException_WhenCartIdentifierIsEmpty()
    {
        var repository =
            new FakeCartRepository(
                null);

        var handler =
            new GetCartQueryHandler(
                repository);

        await Assert.ThrowsAsync<ArgumentException>(
            () => handler.HandleAsync(
                new GetCartQuery(
                    Guid.Empty)));

        Assert.Equal(
            0,
            repository.GetByIdCallCount);
    }

    private sealed class FakeCartRepository
        : ICartRepository
    {
        private readonly Cart? _cart;

        public int GetByIdCallCount { get; private set; }

        public FakeCartRepository(
            Cart? cart)
        {
            _cart =
                cart;
        }

        public Task<Cart?> GetByIdAsync(
            CartId cartId,
            CancellationToken cancellationToken = default)
        {
            GetByIdCallCount++;

            return Task.FromResult(
                _cart);
        }
    }
}