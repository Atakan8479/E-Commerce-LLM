using ECommerce.FlashSaleOrchestrator.Domain.Products;

namespace ECommerce.FlashSaleOrchestrator.Domain.FlashSales;

public sealed class FlashSaleEligibleProduct
{
    public ProductId ProductId { get; }

    private FlashSaleEligibleProduct(
        ProductId productId)
    {
        ArgumentNullException.ThrowIfNull(productId);

        ProductId = productId;
    }

    internal static FlashSaleEligibleProduct Create(
        ProductId productId)
    {
        return new FlashSaleEligibleProduct(productId);
    }
}