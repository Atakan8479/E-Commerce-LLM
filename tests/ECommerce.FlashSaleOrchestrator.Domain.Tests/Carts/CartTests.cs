using ECommerce.FlashSaleOrchestrator.Domain.Carts;
using ECommerce.FlashSaleOrchestrator.Domain.Carts.Exceptions;
using ECommerce.FlashSaleOrchestrator.Domain.Products;

namespace ECommerce.FlashSaleOrchestrator.Domain.Tests.Carts;

public sealed class CartTests
{
    [Fact]
    public void Create_ShouldCreateEmptyCart_WithProvidedIdentifier()
    {
        var cartId = CartId.New();

        var cart =
            Cart.Create(cartId);

        Assert.Equal(
            cartId,
            cart.Id);

        Assert.True(
            cart.IsEmpty);

        Assert.Empty(
            cart.Items);
    }

    [Fact]
    public void Create_ShouldThrowArgumentNullException_WhenIdentifierIsNull()
    {
        var exception =
            Assert.Throws<ArgumentNullException>(
                () => Cart.Create(null!));

        Assert.Equal(
            "id",
            exception.ParamName);
    }

    [Fact]
    public void AddItem_ShouldAddProductToCart()
    {
        var cart =
            Cart.Create(CartId.New());

        var productId =
            ProductId.New();

        cart.AddItem(
            productId,
            2);

        var item =
            Assert.Single(cart.Items);

        Assert.Equal(
            productId,
            item.ProductId);

        Assert.Equal(
            2,
            item.Quantity);

        Assert.False(
            cart.IsEmpty);
    }

    [Fact]
    public void AddItem_ShouldIncreaseQuantity_WhenProductAlreadyExists()
    {
        var cart =
            Cart.Create(CartId.New());

        var productId =
            ProductId.New();

        cart.AddItem(
            productId,
            2);

        cart.AddItem(
            productId,
            3);

        var item =
            Assert.Single(cart.Items);

        Assert.Equal(
            5,
            item.Quantity);
    }

    [Fact]
    public void AddItem_ShouldThrowArgumentNullException_WhenProductIdIsNull()
    {
        var cart =
            Cart.Create(CartId.New());

        var exception =
            Assert.Throws<ArgumentNullException>(
                () => cart.AddItem(
                    null!,
                    1));

        Assert.Equal(
            "productId",
            exception.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AddItem_ShouldThrowArgumentOutOfRangeException_WhenQuantityIsNotPositive(
        int quantity)
    {
        var cart =
            Cart.Create(CartId.New());

        Assert.Throws<ArgumentOutOfRangeException>(
            () => cart.AddItem(
                ProductId.New(),
                quantity));

        Assert.Empty(
            cart.Items);
    }

    [Fact]
    public void RemoveItem_ShouldRemoveExistingProduct()
    {
        var cart =
            Cart.Create(CartId.New());

        var productId =
            ProductId.New();

        cart.AddItem(
            productId,
            2);

        cart.RemoveItem(productId);

        Assert.True(
            cart.IsEmpty);

        Assert.Empty(
            cart.Items);
    }

    [Fact]
    public void RemoveItem_ShouldThrowCartItemNotFoundException_WhenProductDoesNotExist()
    {
        var cartId =
            CartId.New();

        var productId =
            ProductId.New();

        var cart =
            Cart.Create(cartId);

        var exception =
            Assert.Throws<CartItemNotFoundException>(
                () => cart.RemoveItem(productId));

        Assert.Equal(
            cartId,
            exception.CartId);

        Assert.Equal(
            productId,
            exception.ProductId);
    }

    [Fact]
    public void ChangeItemQuantity_ShouldReplaceExistingQuantity()
    {
        var cart =
            Cart.Create(CartId.New());

        var productId =
            ProductId.New();

        cart.AddItem(
            productId,
            2);

        cart.ChangeItemQuantity(
            productId,
            5);

        var item =
            Assert.Single(cart.Items);

        Assert.Equal(
            5,
            item.Quantity);
    }

    [Fact]
    public void ChangeItemQuantity_ShouldThrowCartItemNotFoundException_WhenProductDoesNotExist()
    {
        var cart =
            Cart.Create(CartId.New());

        var productId =
            ProductId.New();

        Assert.Throws<CartItemNotFoundException>(
            () => cart.ChangeItemQuantity(
                productId,
                3));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ChangeItemQuantity_ShouldThrowArgumentOutOfRangeException_WhenQuantityIsNotPositive(
        int quantity)
    {
        var cart =
            Cart.Create(CartId.New());

        var productId =
            ProductId.New();

        cart.AddItem(
            productId,
            2);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => cart.ChangeItemQuantity(
                productId,
                quantity));

        Assert.Equal(
            2,
            Assert.Single(cart.Items).Quantity);
    }

    [Fact]
    public void Contains_ShouldIndicateWhetherProductExistsInCart()
    {
        var cart =
            Cart.Create(CartId.New());

        var productId =
            ProductId.New();

        var anotherProductId =
            ProductId.New();

        cart.AddItem(
            productId,
            1);

        Assert.True(
            cart.Contains(productId));

        Assert.False(
            cart.Contains(anotherProductId));
    }
}