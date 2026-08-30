namespace ECommerce.FlashSaleOrchestrator.Domain.Products;

public sealed record ProductCategory
{
    public const int MaxLength = 100;

    public static ProductCategory Uncategorized { get; } =
        new("uncategorized");

    public string Value { get; }

    private ProductCategory(string value)
    {
        Value = value;
    }

    public static ProductCategory From(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var normalizedValue =
            value.Trim().ToLowerInvariant();

        if (normalizedValue.Length == 0)
        {
            throw new ArgumentException(
                "Product category cannot be empty.",
                nameof(value));
        }

        if (normalizedValue.Length > MaxLength)
        {
            throw new ArgumentException(
                $"Product category cannot exceed {MaxLength} characters.",
                nameof(value));
        }

        return new ProductCategory(
            normalizedValue);
    }

    public override string ToString()
    {
        return Value;
    }
}