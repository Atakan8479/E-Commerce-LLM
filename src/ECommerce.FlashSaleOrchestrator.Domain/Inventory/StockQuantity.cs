namespace ECommerce.FlashSaleOrchestrator.Domain.Inventory;

public sealed record StockQuantity
{
    public static StockQuantity Zero { get; } = new(0);

    public int Value { get; }

    private StockQuantity(int value)
    {
        Value = value;
    }

    public static StockQuantity From(int value)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                value,
                "Stock quantity cannot be negative.");
        }

        return new StockQuantity(value);
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}