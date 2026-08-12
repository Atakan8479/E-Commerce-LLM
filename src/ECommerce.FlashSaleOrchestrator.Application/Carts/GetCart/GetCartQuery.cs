using ECommerce.FlashSaleOrchestrator.Application.Abstractions.Messaging;

namespace ECommerce.FlashSaleOrchestrator.Application.Carts.GetCart;

public sealed record GetCartQuery(
    Guid CartId)
    : IQuery<CartResult?>;