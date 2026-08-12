namespace ECommerce.FlashSaleOrchestrator.Domain.Carts;

public sealed record CartId
{
    public Guid Value { get; }

    private CartId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                "Cart identifier cannot be empty.",
                nameof(value));
        }

        Value = value;
    }

    public static CartId New()
    {
        return new CartId(Guid.NewGuid());
    }

    public static CartId From(Guid value)
    {
        return new CartId(value);
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}