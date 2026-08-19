namespace ECommerce.FlashSaleOrchestrator.Application.Abstractions.Messaging;

public interface IIntegrationEventProcessor<in TEvent>
    where TEvent : class, IIntegrationEvent
{
    Task<IntegrationEventProcessingResult> ProcessAsync(
        TEvent integrationEvent,
        CancellationToken cancellationToken = default);
}