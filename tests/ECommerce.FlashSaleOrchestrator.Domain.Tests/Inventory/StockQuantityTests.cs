using ECommerce.FlashSaleOrchestrator.Domain.Inventory;

namespace ECommerce.FlashSaleOrchestrator.Domain.Tests.Inventory;

public sealed class StockQuantityTests
{
    [Fact]
    public void From_ShouldPreserveProvidedQuantity()
    {
        var quantity = StockQuantity.From(10);

        Assert.Equal(10, quantity.Value);
    }

    [Fact]
    public void From_ShouldAllowZeroQuantity()
    {
        var quantity = StockQuantity.From(0);

        Assert.Equal(0, quantity.Value);
    }

    [Fact]
    public void From_ShouldThrowArgumentOutOfRangeException_WhenQuantityIsNegative()
    {
        var exception =
            Assert.Throws<ArgumentOutOfRangeException>(
                () => StockQuantity.From(-1));

        Assert.Equal("value", exception.ParamName);

        Assert.Contains(
            "Stock quantity cannot be negative.",
            exception.Message);
    }

    [Fact]
    public void StockQuantities_ShouldBeEqual_WhenValuesAreEqual()
    {
        var firstQuantity =
            StockQuantity.From(10);

        var secondQuantity =
            StockQuantity.From(10);

        Assert.Equal(
            firstQuantity,
            secondQuantity);

        Assert.True(
            firstQuantity == secondQuantity);
    }

    [Fact]
    public void ToString_ShouldReturnUnderlyingQuantityAsString()
    {
        var quantity =
            StockQuantity.From(10);

        var result =
            quantity.ToString();

        Assert.Equal("10", result);
    }
}