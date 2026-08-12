namespace ECommerce.FlashSaleOrchestrator.Domain.FlashSales;

public sealed record FlashSaleId
{
    public Guid Value { get; }

    private FlashSaleId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                "Flash sale identifier cannot be empty.",
                nameof(value));
        }

        Value = value;
    }

    public static FlashSaleId New()
    {
        return new FlashSaleId(Guid.NewGuid());
    }

    public static FlashSaleId From(Guid value)
    {
        return new FlashSaleId(value);
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}