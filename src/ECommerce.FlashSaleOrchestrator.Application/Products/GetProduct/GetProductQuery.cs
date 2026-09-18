using ECommerce.FlashSaleOrchestrator.Application
    .Abstractions.Messaging;

namespace ECommerce.FlashSaleOrchestrator.Application
    .Products.GetProduct;

public sealed record GetProductQuery(
    Guid ProductId)
    : IQuery<ProductResult?>;