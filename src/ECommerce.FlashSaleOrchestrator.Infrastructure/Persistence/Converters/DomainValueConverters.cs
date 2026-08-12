using ECommerce.FlashSaleOrchestrator.Domain.Carts;
using ECommerce.FlashSaleOrchestrator.Domain.FlashSales;
using ECommerce.FlashSaleOrchestrator.Domain.Inventory;
using ECommerce.FlashSaleOrchestrator.Domain.Products;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ECommerce.FlashSaleOrchestrator.Infrastructure.Persistence.Converters;

internal sealed class ProductIdValueConverter
    : ValueConverter<ProductId, Guid>
{
    public ProductIdValueConverter()
        : base(
            productId => productId.Value,
            value => ProductId.From(value))
    {
    }
}

internal sealed class ProductNameValueConverter
    : ValueConverter<ProductName, string>
{
    public ProductNameValueConverter()
        : base(
            productName => productName.Value,
            value => ProductName.From(value))
    {
    }
}

internal sealed class StockQuantityValueConverter
    : ValueConverter<StockQuantity, int>
{
    public StockQuantityValueConverter()
        : base(
            quantity => quantity.Value,
            value => StockQuantity.From(value))
    {
    }
}

internal sealed class CartIdValueConverter
    : ValueConverter<CartId, Guid>
{
    public CartIdValueConverter()
        : base(
            cartId => cartId.Value,
            value => CartId.From(value))
    {
    }
}

internal sealed class FlashSaleIdValueConverter
    : ValueConverter<FlashSaleId, Guid>
{
    public FlashSaleIdValueConverter()
        : base(
            flashSaleId => flashSaleId.Value,
            value => FlashSaleId.From(value))
    {
    }
}