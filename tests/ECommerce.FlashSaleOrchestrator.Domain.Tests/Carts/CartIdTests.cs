using ECommerce.FlashSaleOrchestrator.Domain.Carts;

namespace ECommerce.FlashSaleOrchestrator.Domain.Tests.Carts;

public sealed class CartIdTests
{
    [Fact]
    public void New_ShouldCreateNonEmptyIdentifier()
    {
        var cartId = CartId.New();

        Assert.NotEqual(
            Guid.Empty,
            cartId.Value);
    }

    [Fact]
    public void New_ShouldCreateDifferentIdentifiers_ForDifferentCalls()
    {
        var firstCartId = CartId.New();
        var secondCartId = CartId.New();

        Assert.NotEqual(
            firstCartId,
            secondCartId);
    }

    [Fact]
    public void From_ShouldPreserveProvidedIdentifier()
    {
        var value = Guid.NewGuid();

        var cartId =
            CartId.From(value);

        Assert.Equal(
            value,
            cartId.Value);
    }

    [Fact]
    public void From_ShouldThrowArgumentException_WhenIdentifierIsEmpty()
    {
        var exception =
            Assert.Throws<ArgumentException>(
                () => CartId.From(Guid.Empty));

        Assert.Equal(
            "value",
            exception.ParamName);
    }

    [Fact]
    public void CartIds_ShouldBeEqual_WhenValuesAreEqual()
    {
        var value = Guid.NewGuid();

        var firstCartId =
            CartId.From(value);

        var secondCartId =
            CartId.From(value);

        Assert.Equal(
            firstCartId,
            secondCartId);
    }

    [Fact]
    public void ToString_ShouldReturnUnderlyingGuidAsString()
    {
        var value = Guid.NewGuid();

        var cartId =
            CartId.From(value);

        Assert.Equal(
            value.ToString(),
            cartId.ToString());
    }
}