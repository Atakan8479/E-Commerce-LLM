using ECommerce.FlashSaleOrchestrator.Application.Abstractions.Messaging;
using ECommerce.FlashSaleOrchestrator.Application.Abstractions.Persistence;
using ECommerce.FlashSaleOrchestrator.Domain.Carts;

namespace ECommerce.FlashSaleOrchestrator.Application.Carts.GetCart;

public sealed class GetCartQueryHandler
    : IQueryHandler<GetCartQuery, CartResult?>
{
    private readonly ICartRepository _cartRepository;

    public GetCartQueryHandler(
        ICartRepository cartRepository)
    {
        _cartRepository =
            cartRepository
            ?? throw new ArgumentNullException(
                nameof(cartRepository));
    }

    public async Task<CartResult?> HandleAsync(
        GetCartQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var cartId =
            CartId.From(query.CartId);

        var cart =
            await _cartRepository.GetByIdAsync(
                cartId,
                cancellationToken);

        if (cart is null)
        {
            return null;
        }

        var items =
            cart.Items
                .Select(
                    item =>
                        new CartItemResult(
                            item.ProductId.Value,
                            item.Quantity))
                .ToArray();

        return new CartResult(
            cart.Id.Value,
            items);
    }
}