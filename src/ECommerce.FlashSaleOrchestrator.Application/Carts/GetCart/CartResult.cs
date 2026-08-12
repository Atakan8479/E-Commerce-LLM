namespace ECommerce.FlashSaleOrchestrator.Application.Carts.GetCart;

public sealed record CartResult(
    Guid CartId,
    IReadOnlyList<CartItemResult> Items);

public sealed record CartItemResult(
    Guid ProductId,
    int Quantity);