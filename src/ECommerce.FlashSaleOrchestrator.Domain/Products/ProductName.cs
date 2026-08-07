namespace ECommerce.FlashSaleOrchestrator.Domain.Products;

public sealed record ProductName
{
    public const int MaxLength = 200;

    public string Value { get; }

    private ProductName(string value)
    {
        Value = value;
    }

    public static ProductName From(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var normalizedValue = value.Trim();

        if (normalizedValue.Length == 0)
        {
            throw new ArgumentException(
                "Product name cannot be empty.",
                nameof(value));
        }

        if (normalizedValue.Length > MaxLength)
        {
            throw new ArgumentException(
                $"Product name cannot exceed {MaxLength} characters.",
                nameof(value));
        }

        return new ProductName(normalizedValue);
    }

    public override string ToString()
    {
        return Value;
    }
}