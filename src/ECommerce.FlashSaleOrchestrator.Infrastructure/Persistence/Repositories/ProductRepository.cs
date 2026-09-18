using ECommerce.FlashSaleOrchestrator.Application
    .Abstractions.Persistence;
using ECommerce.FlashSaleOrchestrator.Domain.Products;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.FlashSaleOrchestrator.Infrastructure
    .Persistence.Repositories;

public sealed class ProductRepository
    : IProductRepository
{
    private readonly FlashSaleOrchestratorDbContext
        _dbContext;

    public ProductRepository(
        FlashSaleOrchestratorDbContext dbContext)
    {
        _dbContext =
            dbContext
            ?? throw new ArgumentNullException(
                nameof(dbContext));
    }

    public Task<Product?> GetByIdAsync(
        ProductId productId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            productId);

        return _dbContext.Products
            .AsNoTracking()
            .SingleOrDefaultAsync(
                product =>
                    product.Id == productId,
                cancellationToken);
    }
}