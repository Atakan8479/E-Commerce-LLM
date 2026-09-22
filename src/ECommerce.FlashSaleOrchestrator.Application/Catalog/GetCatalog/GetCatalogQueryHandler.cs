using ECommerce.FlashSaleOrchestrator.Application
    .Abstractions.Messaging;
using ECommerce.FlashSaleOrchestrator.Application
    .Abstractions.Persistence;

namespace ECommerce.FlashSaleOrchestrator.Application
    .Catalog.GetCatalog;

public sealed class GetCatalogQueryHandler
    : IQueryHandler<
        GetCatalogQuery,
        IReadOnlyList<CatalogItemResult>>
{
    private readonly ICatalogReadRepository
        _catalogReadRepository;

    public GetCatalogQueryHandler(
        ICatalogReadRepository catalogReadRepository)
    {
        _catalogReadRepository =
            catalogReadRepository
            ?? throw new ArgumentNullException(
                nameof(catalogReadRepository));
    }

    public async Task<IReadOnlyList<CatalogItemResult>>
        HandleAsync(
            GetCatalogQuery query,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            query);

        var items =
            await _catalogReadRepository.ListAsync(
                cancellationToken);

        return items
            .Select(
                item =>
                    new CatalogItemResult(
                        item.ProductId,
                        item.Name,
                        item.Category,
                        item.AvailableQuantity,
                        item.AvailableQuantity == 0))
            .OrderBy(
                item =>
                    item.Name,
                StringComparer.OrdinalIgnoreCase)
            .ThenBy(
                item =>
                    item.ProductId)
            .ToArray();
    }
}