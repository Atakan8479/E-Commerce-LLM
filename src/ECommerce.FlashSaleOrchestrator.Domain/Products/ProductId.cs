namespace ECommerce.FlashSaleOrchestrator.Domain.Products;

public sealed record ProductId
{
    public Guid Value { get; }

    private ProductId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                "Product identifier cannot be empty.",
                nameof(value));
        }

        Value = value;
    }

    public static ProductId New()
    {
        return new ProductId(Guid.NewGuid());
    }

    public static ProductId From(Guid value)
    {
        return new ProductId(value);
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}