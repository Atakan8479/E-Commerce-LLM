using ECommerce.FlashSaleOrchestrator.Domain.Products;

namespace ECommerce.FlashSaleOrchestrator.Domain.Carts;

public sealed class CartItem
{
    public ProductId ProductId { get; }

    public int Quantity { get; private set; }

    internal CartItem(
        ProductId productId,
        int quantity)
    {
        ArgumentNullException.ThrowIfNull(productId);

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantity),
                quantity,
                "Cart item quantity must be greater than zero.");
        }

        ProductId = productId;
        Quantity = quantity;
    }

    internal void IncreaseQuantity(int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantity),
                quantity,
                "Cart item quantity increase must be greater than zero.");
        }

        Quantity = checked(Quantity + quantity);
    }

    internal void ChangeQuantity(int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantity),
                quantity,
                "Cart item quantity must be greater than zero.");
        }

        Quantity = quantity;
    }
}