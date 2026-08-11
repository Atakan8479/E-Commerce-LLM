using ECommerce.FlashSaleOrchestrator.Domain.Products;

namespace ECommerce.FlashSaleOrchestrator.Domain.Tests.Products;

public sealed class ProductNameTests
{
    [Fact]
    public void From_ShouldPreserveValidProductName()
    {
        const string value = "Wireless Mechanical Keyboard";

        var productName = ProductName.From(value);

        Assert.Equal(value, productName.Value);
    }

    [Fact]
    public void From_ShouldTrimLeadingAndTrailingWhitespace()
    {
        const string value = "  Wireless Mechanical Keyboard  ";

        var productName = ProductName.From(value);

        Assert.Equal(
            "Wireless Mechanical Keyboard",
            productName.Value);
    }

    [Fact]
    public void From_ShouldThrowArgumentNullException_WhenValueIsNull()
    {
        var exception = Assert.Throws<ArgumentNullException>(
            () => ProductName.From(null!));

        Assert.Equal("value", exception.ParamName);
    }

    [Fact]
    public void From_ShouldThrowArgumentException_WhenValueIsEmpty()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => ProductName.From(string.Empty));

        Assert.Equal("value", exception.ParamName);

        Assert.Contains(
            "Product name cannot be empty.",
            exception.Message);
    }

    [Fact]
    public void From_ShouldThrowArgumentException_WhenValueContainsOnlyWhitespace()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => ProductName.From("   "));

        Assert.Equal("value", exception.ParamName);

        Assert.Contains(
            "Product name cannot be empty.",
            exception.Message);
    }

    [Fact]
    public void From_ShouldThrowArgumentException_WhenValueExceedsMaximumLength()
    {
        var value = new string(
            'a',
            ProductName.MaxLength + 1);

        var exception = Assert.Throws<ArgumentException>(
            () => ProductName.From(value));

        Assert.Equal("value", exception.ParamName);

        Assert.Contains(
            $"Product name cannot exceed {ProductName.MaxLength} characters.",
            exception.Message);
    }

    [Fact]
    public void ProductNames_ShouldBeEqual_WhenNormalizedValuesAreEqual()
    {
        var firstProductName =
            ProductName.From("Wireless Mechanical Keyboard");

        var secondProductName =
            ProductName.From("  Wireless Mechanical Keyboard  ");

        Assert.Equal(
            firstProductName,
            secondProductName);

        Assert.True(
            firstProductName == secondProductName);
    }

    [Fact]
    public void ToString_ShouldReturnNormalizedProductName()
    {
        var productName =
            ProductName.From("  Wireless Mechanical Keyboard  ");

        var result = productName.ToString();

        Assert.Equal(
            "Wireless Mechanical Keyboard",
            result);
    }
}