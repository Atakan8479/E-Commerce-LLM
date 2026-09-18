namespace ECommerce.FlashSaleOrchestrator.Application
    .Products.GetProduct;

public sealed record ProductResult(
    Guid ProductId,
    string Name,
    string Category);