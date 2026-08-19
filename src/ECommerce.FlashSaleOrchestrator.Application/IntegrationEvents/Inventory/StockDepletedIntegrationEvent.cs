using ECommerce.FlashSaleOrchestrator.Application.Abstractions.Messaging;

namespace ECommerce.FlashSaleOrchestrator.Application.IntegrationEvents.Inventory;

public sealed record StockDepletedIntegrationEvent(
    Guid EventId,
    DateTime OccurredAtUtc,
    Guid ProductId)
    : IIntegrationEvent
{
    public const string EventTypeName =
        "stock-depleted";

    public string EventType =>
        EventTypeName;
}