using ECommerce.FlashSaleOrchestrator.Application.Abstractions.AlternativeCandidates;
using ECommerce.FlashSaleOrchestrator.Application.AlternativeCandidates;
using ECommerce.FlashSaleOrchestrator.Domain.Inventory;
using ECommerce.FlashSaleOrchestrator.Domain.Products;
using ECommerce.FlashSaleOrchestrator.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.FlashSaleOrchestrator.Infrastructure.AlternativeCandidates;

public sealed class SqlAlternativeCandidateProvider
    : IAlternativeCandidateProvider
{
    private readonly FlashSaleOrchestratorDbContext _dbContext;

    public SqlAlternativeCandidateProvider(
        FlashSaleOrchestratorDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<AlternativeCandidate>> GetCandidatesAsync(
        Guid depletedProductId,
        int limit,
        CancellationToken cancellationToken = default)
    {
        if (depletedProductId == Guid.Empty)
        {
            throw new ArgumentException(
                "Depleted product identifier cannot be empty.",
                nameof(depletedProductId));
        }

        if (limit <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(limit),
                limit,
                "Candidate limit must be greater than zero.");
        }

        var excludedProductId =
            ProductId.From(depletedProductId);

        var depletedProduct =
            await _dbContext.Products
                .AsNoTracking()
                .Where(
                    product =>
                        product.Id == excludedProductId)
                .Select(
                    product =>
                        new
                        {
                            product.Category
                        })
                .SingleOrDefaultAsync(
                    cancellationToken);

        if (depletedProduct is null ||
            depletedProduct.Category ==
            ProductCategory.Uncategorized)
        {
            return Array.Empty<AlternativeCandidate>();
        }

        var depletedCategory =
            depletedProduct.Category;

        var candidates =
            await (
                from product in
                    _dbContext.Products.AsNoTracking()
                join inventoryItem in
                    _dbContext.InventoryItems.AsNoTracking()
                    on product.Id equals inventoryItem.ProductId
                where product.Id != excludedProductId
                      && product.Category == depletedCategory
                      && inventoryItem.AvailableQuantity != StockQuantity.Zero
                orderby inventoryItem.AvailableQuantity descending,
                    product.Id
                select new
                {
                    product.Id,
                    product.Name,
                    product.Category,
                    inventoryItem.AvailableQuantity
                })
            .Take(limit)
            .ToListAsync(
                cancellationToken);

        return candidates
            .Select(
                candidate =>
                    new AlternativeCandidate(
                        candidate.Id.Value,
                        candidate.Name.Value,
                        candidate.Category.Value,
                        candidate.AvailableQuantity.Value))
            .ToArray();
    }
}