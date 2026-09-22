using ECommerce.FlashSaleOrchestrator.Application
    .Abstractions.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.FlashSaleOrchestrator.Infrastructure
    .Persistence.Repositories;

public sealed class CatalogReadRepository
    : ICatalogReadRepository
{
    private readonly FlashSaleOrchestratorDbContext
        _dbContext;

    public CatalogReadRepository(
        FlashSaleOrchestratorDbContext dbContext)
    {
        _dbContext =
            dbContext
            ?? throw new ArgumentNullException(
                nameof(dbContext));
    }

    public async Task<IReadOnlyList<CatalogItemReadModel>>
        ListAsync(
            CancellationToken cancellationToken = default)
    {
        var rows =
            await (
                from product in
                    _dbContext.Products.AsNoTracking()
                join inventoryItem in
                    _dbContext.InventoryItems.AsNoTracking()
                    on product.Id
                    equals inventoryItem.ProductId
                select new
                {
                    Product =
                        product,
                    InventoryItem =
                        inventoryItem
                })
            .ToArrayAsync(
                cancellationToken);

        return rows
            .Select(
                row =>
                    new CatalogItemReadModel(
                        row.Product.Id.Value,
                        row.Product.Name.Value,
                        row.Product.Category.Value,
                        row.InventoryItem
                            .AvailableQuantity.Value))
            .ToArray();
    }
}