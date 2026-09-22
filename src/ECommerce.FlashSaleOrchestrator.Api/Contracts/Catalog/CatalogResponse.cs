namespace ECommerce.FlashSaleOrchestrator.Api
    .Contracts.Catalog;

public sealed record CatalogResponse(
    IReadOnlyList<CatalogItemResponse> Items);

public sealed record CatalogItemResponse(
    Guid ProductId,
    string Name,
    string Category,
    int AvailableQuantity,
    bool IsDepleted);