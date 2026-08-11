using ECommerce.FlashSaleOrchestrator.Domain.Abstractions;
using ECommerce.FlashSaleOrchestrator.Domain.Products;

namespace ECommerce.FlashSaleOrchestrator.Domain.Inventory.Events;

public sealed record StockDepletedDomainEvent(
    ProductId ProductId) : IDomainEvent;