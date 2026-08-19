namespace ECommerce.FlashSaleOrchestrator.Application.Abstractions.Messaging;

public interface IIntegrationEvent
{
    Guid EventId { get; }

    DateTime OccurredAtUtc { get; }

    string EventType { get; }
}