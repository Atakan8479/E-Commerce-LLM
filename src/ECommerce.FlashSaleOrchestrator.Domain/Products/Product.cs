namespace ECommerce.FlashSaleOrchestrator.Domain.Products;

public sealed class Product
{
    public ProductId Id { get; }

    public ProductName Name { get; private set; }

    private Product(
        ProductId id,
        ProductName name)
    {
        Id = id;
        Name = name;
    }

    public static Product Create(
        ProductId id,
        ProductName name)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(name);

        return new Product(id, name);
    }

    public void Rename(ProductName name)
    {
        ArgumentNullException.ThrowIfNull(name);

        Name = name;
    }
}