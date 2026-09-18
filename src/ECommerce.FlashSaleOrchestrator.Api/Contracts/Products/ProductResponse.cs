namespace ECommerce.FlashSaleOrchestrator.Api
    .Contracts.Products;

public sealed record ProductResponse(
    Guid ProductId,
    string Name,
    string Category);