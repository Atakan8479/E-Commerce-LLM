namespace ECommerce.FlashSaleOrchestrator.Domain.Products;

public sealed class Product
{
    public ProductId Id { get; }

    public ProductName Name { get; private set; }

    public ProductCategory Category { get; private set; }

    private Product(
        ProductId id,
        ProductName name,
        ProductCategory category)
    {
        Id = id;
        Name = name;
        Category = category;
    }

    public static Product Create(
        ProductId id,
        ProductName name)
    {
        return Create(
            id,
            name,
            ProductCategory.Uncategorized);
    }

    public static Product Create(
        ProductId id,
        ProductName name,
        ProductCategory category)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(category);

        return new Product(
            id,
            name,
            category);
    }

    public void Rename(ProductName name)
    {
        ArgumentNullException.ThrowIfNull(name);

        Name = name;
    }

    public void ChangeCategory(
        ProductCategory category)
    {
        ArgumentNullException.ThrowIfNull(category);

        Category = category;
    }
}