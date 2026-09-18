using System.ComponentModel.DataAnnotations;

namespace ECommerce.FlashSaleOrchestrator.Api
    .Contracts.Inventory;

public sealed record DecreaseStockRequest
{
    [Range(
        1,
        int.MaxValue,
        ErrorMessage =
            "Quantity must be greater than zero.")]
    public int Quantity
    {
        get;
        init;
    }
}