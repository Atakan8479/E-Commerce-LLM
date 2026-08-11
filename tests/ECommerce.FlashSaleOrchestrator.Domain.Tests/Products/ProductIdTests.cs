using ECommerce.FlashSaleOrchestrator.Domain.Products;

namespace ECommerce.FlashSaleOrchestrator.Domain.Tests.Products;

public sealed class ProductIdTests
{
    [Fact]
    public void New_ShouldCreateNonEmptyIdentifier()
    {
        var productId = ProductId.New();

        Assert.NotEqual(Guid.Empty, productId.Value);
    }

    [Fact]
    public void New_ShouldCreateDifferentIdentifiers_ForDifferentCalls()
    {
        var firstProductId = ProductId.New();
        var secondProductId = ProductId.New();

        Assert.NotEqual(firstProductId, secondProductId);
        Assert.NotEqual(firstProductId.Value, secondProductId.Value);
    }

    [Fact]
    public void From_ShouldPreserveProvidedIdentifier()
    {
        var value = Guid.NewGuid();

        var productId = ProductId.From(value);

        Assert.Equal(value, productId.Value);
    }

    [Fact]
    public void From_ShouldThrowArgumentException_WhenIdentifierIsEmpty()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => ProductId.From(Guid.Empty));

        Assert.Equal("value", exception.ParamName);
        Assert.Contains(
            "Product identifier cannot be empty.",
            exception.Message);
    }

    [Fact]
    public void ProductIds_ShouldBeEqual_WhenValuesAreEqual()
    {
        var value = Guid.NewGuid();

        var firstProductId = ProductId.From(value);
        var secondProductId = ProductId.From(value);

        Assert.Equal(firstProductId, secondProductId);
        Assert.True(firstProductId == secondProductId);
    }

    [Fact]
    public void ProductIds_ShouldNotBeEqual_WhenValuesAreDifferent()
    {
        var firstProductId = ProductId.From(Guid.NewGuid());
        var secondProductId = ProductId.From(Guid.NewGuid());

        Assert.NotEqual(firstProductId, secondProductId);
        Assert.True(firstProductId != secondProductId);
    }

    [Fact]
    public void ToString_ShouldReturnUnderlyingGuidAsString()
    {
        var value = Guid.NewGuid();
        var productId = ProductId.From(value);

        var result = productId.ToString();

        Assert.Equal(value.ToString(), result);
    }
}