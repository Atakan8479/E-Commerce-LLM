using ECommerce.FlashSaleOrchestrator.Domain.FlashSales;

namespace ECommerce.FlashSaleOrchestrator.Domain.Tests.FlashSales;

public sealed class FlashSaleIdTests
{
    [Fact]
    public void New_ShouldCreateNonEmptyIdentifier()
    {
        var flashSaleId =
            FlashSaleId.New();

        Assert.NotEqual(
            Guid.Empty,
            flashSaleId.Value);
    }

    [Fact]
    public void New_ShouldCreateDifferentIdentifiers_ForDifferentCalls()
    {
        var firstFlashSaleId =
            FlashSaleId.New();

        var secondFlashSaleId =
            FlashSaleId.New();

        Assert.NotEqual(
            firstFlashSaleId,
            secondFlashSaleId);
    }

    [Fact]
    public void From_ShouldPreserveProvidedIdentifier()
    {
        var value =
            Guid.NewGuid();

        var flashSaleId =
            FlashSaleId.From(value);

        Assert.Equal(
            value,
            flashSaleId.Value);
    }

    [Fact]
    public void From_ShouldThrowArgumentException_WhenIdentifierIsEmpty()
    {
        var exception =
            Assert.Throws<ArgumentException>(
                () => FlashSaleId.From(Guid.Empty));

        Assert.Equal(
            "value",
            exception.ParamName);
    }

    [Fact]
    public void FlashSaleIds_ShouldBeEqual_WhenValuesAreEqual()
    {
        var value =
            Guid.NewGuid();

        var firstFlashSaleId =
            FlashSaleId.From(value);

        var secondFlashSaleId =
            FlashSaleId.From(value);

        Assert.Equal(
            firstFlashSaleId,
            secondFlashSaleId);
    }

    [Fact]
    public void ToString_ShouldReturnUnderlyingGuidAsString()
    {
        var value =
            Guid.NewGuid();

        var flashSaleId =
            FlashSaleId.From(value);

        Assert.Equal(
            value.ToString(),
            flashSaleId.ToString());
    }
}