using ECommerce.FlashSaleOrchestrator.Domain.Products;

namespace ECommerce.FlashSaleOrchestrator.Domain.Carts.Exceptions;

public sealed class CartItemNotFoundException : InvalidOperationException
{
    public CartId CartId { get; }

    public ProductId ProductId { get; }

    public CartItemNotFoundException(
        CartId cartId,
        ProductId productId)
        : base(
            $"Product '{productId}' was not found in cart '{cartId}'.")
    {
        CartId = cartId;
        ProductId = productId;
    }
}