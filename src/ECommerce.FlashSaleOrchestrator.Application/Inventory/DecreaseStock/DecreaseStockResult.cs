namespace ECommerce.FlashSaleOrchestrator.Application.Inventory.DecreaseStock;

public sealed record DecreaseStockResult(
    Guid ProductId,
    int RemainingQuantity,
    bool IsDepleted);