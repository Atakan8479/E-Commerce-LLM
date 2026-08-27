namespace ECommerce.FlashSaleOrchestrator.Application.Abstractions.Messaging;

public interface IIntegrationEventHandler<in TEvent>
    where TEvent : class
{
    Task HandleAsync(
        TEvent integrationEvent,
        CancellationToken cancellationToken = default);
}