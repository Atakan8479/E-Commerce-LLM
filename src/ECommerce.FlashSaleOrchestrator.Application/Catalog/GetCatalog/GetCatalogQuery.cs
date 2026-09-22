using ECommerce.FlashSaleOrchestrator.Application
    .Abstractions.Messaging;

namespace ECommerce.FlashSaleOrchestrator.Application
    .Catalog.GetCatalog;

public sealed record GetCatalogQuery
    : IQuery<IReadOnlyList<CatalogItemResult>>;