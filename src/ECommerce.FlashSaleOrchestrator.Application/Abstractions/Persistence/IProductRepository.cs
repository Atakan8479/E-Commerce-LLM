using ECommerce.FlashSaleOrchestrator.Domain.Products;

namespace ECommerce.FlashSaleOrchestrator.Application
    .Abstractions.Persistence;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(
        ProductId productId,
        CancellationToken cancellationToken = default);
}