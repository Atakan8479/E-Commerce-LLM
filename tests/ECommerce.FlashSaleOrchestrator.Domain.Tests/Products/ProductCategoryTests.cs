using ECommerce.FlashSaleOrchestrator.Domain.Products;

namespace ECommerce.FlashSaleOrchestrator.Domain.Tests.Products;

public sealed class ProductCategoryTests
{
    [Fact]
    public void From_ShouldNormalizeCategory()
    {
        var category =
            ProductCategory.From(
                "  Mouse  ");

        Assert.Equal(
            "mouse",
            category.Value);
    }

    [Fact]
    public void From_ShouldCreateEqualCategories_WhenValuesDifferOnlyByCasing()
    {
        var firstCategory =
            ProductCategory.From(
                "Mouse");

        var secondCategory =
            ProductCategory.From(
                "MOUSE");

        Assert.Equal(
            firstCategory,
            secondCategory);
    }

    [Fact]
    public void From_ShouldThrowArgumentNullException_WhenValueIsNull()
    {
        var exception =
            Assert.Throws<ArgumentNullException>(
                () =>
                    ProductCategory.From(
                        null!));

        Assert.Equal(
            "value",
            exception.ParamName);
    }

    [Fact]
    public void From_ShouldThrowArgumentException_WhenValueIsWhitespace()
    {
        var exception =
            Assert.Throws<ArgumentException>(
                () =>
                    ProductCategory.From(
                        "   "));

        Assert.Equal(
            "value",
            exception.ParamName);
    }

    [Fact]
    public void From_ShouldThrowArgumentException_WhenValueExceedsMaximumLength()
    {
        var value =
            new string(
                'a',
                ProductCategory.MaxLength + 1);

        var exception =
            Assert.Throws<ArgumentException>(
                () =>
                    ProductCategory.From(
                        value));

        Assert.Equal(
            "value",
            exception.ParamName);
    }
}