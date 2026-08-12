using ECommerce.FlashSaleOrchestrator.Domain.Carts.Exceptions;
using ECommerce.FlashSaleOrchestrator.Domain.Products;

namespace ECommerce.FlashSaleOrchestrator.Domain.Carts;

public sealed class Cart
{
    private readonly List<CartItem> _items = [];

    public CartId Id { get; }

    public IReadOnlyCollection<CartItem> Items =>
        _items.AsReadOnly();

    public bool IsEmpty =>
        _items.Count == 0;

    private Cart(CartId id)
    {
        Id = id;
    }

    public static Cart Create(CartId id)
    {
        ArgumentNullException.ThrowIfNull(id);

        return new Cart(id);
    }

    public void AddItem(
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

        var existingItem =
            _items.SingleOrDefault(
                item => item.ProductId == productId);

        if (existingItem is not null)
        {
            existingItem.IncreaseQuantity(quantity);
            return;
        }

        _items.Add(
            new CartItem(
                productId,
                quantity));
    }

    public void RemoveItem(ProductId productId)
    {
        ArgumentNullException.ThrowIfNull(productId);

        var item =
            _items.SingleOrDefault(
                item => item.ProductId == productId);

        if (item is null)
        {
            throw new CartItemNotFoundException(
                Id,
                productId);
        }

        _items.Remove(item);
    }

    public void ChangeItemQuantity(
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

        var item =
            _items.SingleOrDefault(
                item => item.ProductId == productId);

        if (item is null)
        {
            throw new CartItemNotFoundException(
                Id,
                productId);
        }

        item.ChangeQuantity(quantity);
    }

    public bool Contains(ProductId productId)
    {
        ArgumentNullException.ThrowIfNull(productId);

        return _items.Any(
            item => item.ProductId == productId);
    }
}