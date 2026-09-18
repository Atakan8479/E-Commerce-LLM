using ECommerce.FlashSaleOrchestrator.Application
    .Abstractions.Messaging;
using ECommerce.FlashSaleOrchestrator.Application
    .Abstractions.Persistence;
using ECommerce.FlashSaleOrchestrator.Domain.Products;

namespace ECommerce.FlashSaleOrchestrator.Application
    .Products.GetProduct;

public sealed class GetProductQueryHandler
    : IQueryHandler<GetProductQuery, ProductResult?>
{
    private readonly IProductRepository
        _productRepository;

    public GetProductQueryHandler(
        IProductRepository productRepository)
    {
        _productRepository =
            productRepository
            ?? throw new ArgumentNullException(
                nameof(productRepository));
    }

    public async Task<ProductResult?> HandleAsync(
        GetProductQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            query);

        var productId =
            ProductId.From(
                query.ProductId);

        var product =
            await _productRepository.GetByIdAsync(
                productId,
                cancellationToken);

        if (product is null)
        {
            return null;
        }

        return new ProductResult(
            product.Id.Value,
            product.Name.Value,
            product.Category.Value);
    }
}