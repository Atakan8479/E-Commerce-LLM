using ECommerce.FlashSaleOrchestrator.Application.Abstractions.Messaging;

namespace ECommerce.FlashSaleOrchestrator.Application.Inventory.DecreaseStock;

public sealed record DecreaseStockCommand(
    Guid ProductId,
    int Quantity)
    : ICommand<DecreaseStockResult>;