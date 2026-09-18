namespace ECommerce.FlashSaleOrchestrator.Api
    .Contracts.Carts;

public sealed record CartResponse(
    Guid CartId,
    IReadOnlyList<CartItemResponse> Items);

public sealed record CartItemResponse(
    Guid ProductId,
    int Quantity);