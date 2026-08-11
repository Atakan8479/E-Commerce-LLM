using ECommerce.FlashSaleOrchestrator.Domain.Products;

namespace ECommerce.FlashSaleOrchestrator.Domain.Tests.Products;

public sealed class ProductTests
{
    [Fact]
    public void Create_ShouldCreateProduct_WithProvidedIdentifierAndName()
    {
        var productId = ProductId.New();
        var productName = ProductName.From(
            "Wireless Mechanical Keyboard");

        var product = Product.Create(
            productId,
            productName);

        Assert.Equal(productId, product.Id);
        Assert.Equal(productName, product.Name);
    }

    [Fact]
    public void Create_ShouldThrowArgumentNullException_WhenIdentifierIsNull()
    {
        var productName = ProductName.From(
            "Wireless Mechanical Keyboard");

        var exception = Assert.Throws<ArgumentNullException>(
            () => Product.Create(
                null!,
                productName));

        Assert.Equal("id", exception.ParamName);
    }

    [Fact]
    public void Create_ShouldThrowArgumentNullException_WhenNameIsNull()
    {
        var productId = ProductId.New();

        var exception = Assert.Throws<ArgumentNullException>(
            () => Product.Create(
                productId,
                null!));

        Assert.Equal("name", exception.ParamName);
    }

    [Fact]
    public void Rename_ShouldReplaceProductName()
    {
        var product = Product.Create(
            ProductId.New(),
            ProductName.From("Mechanical Keyboard"));

        var newName = ProductName.From(
            "Wireless Mechanical Keyboard");

        product.Rename(newName);

        Assert.Equal(newName, product.Name);
        Assert.Equal(
            "Wireless Mechanical Keyboard",
            product.Name.Value);
    }

    [Fact]
    public void Rename_ShouldNotChangeProductIdentifier()
    {
        var productId = ProductId.New();

        var product = Product.Create(
            productId,
            ProductName.From("Mechanical Keyboard"));

        product.Rename(
            ProductName.From(
                "Wireless Mechanical Keyboard"));

        Assert.Equal(productId, product.Id);
    }

    [Fact]
    public void Rename_ShouldThrowArgumentNullException_WhenNameIsNull()
    {
        var product = Product.Create(
            ProductId.New(),
            ProductName.From("Mechanical Keyboard"));

        var exception = Assert.Throws<ArgumentNullException>(
            () => product.Rename(null!));

        Assert.Equal("name", exception.ParamName);
    }
}