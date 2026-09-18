using ECommerce.FlashSaleOrchestrator.Application
    .Abstractions.Messaging;

namespace ECommerce.FlashSaleOrchestrator.Application
    .Inventory.GetInventory;

public sealed record GetInventoryQuery(
    Guid ProductId)
    : IQuery<InventoryResult?>;