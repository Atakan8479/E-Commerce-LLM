namespace ECommerce.FlashSaleOrchestrator.Application
    .Catalog.GetCatalog;

public sealed record CatalogItemResult(
    Guid ProductId,
    string Name,
    string Category,
    int AvailableQuantity,
    bool IsDepleted);