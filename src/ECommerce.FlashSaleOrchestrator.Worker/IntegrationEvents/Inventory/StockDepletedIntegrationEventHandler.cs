using ECommerce.FlashSaleOrchestrator.Application.Abstractions.Messaging;
using ECommerce.FlashSaleOrchestrator.Application.IntegrationEvents.Inventory;

namespace ECommerce.FlashSaleOrchestrator.Worker.IntegrationEvents.Inventory;

public sealed class StockDepletedIntegrationEventHandler
    : IIntegrationEventHandler<StockDepletedIntegrationEvent>
{
    private readonly ILogger<StockDepletedIntegrationEventHandler>
        _logger;

    public StockDepletedIntegrationEventHandler(
        ILogger<StockDepletedIntegrationEventHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(
            logger);

        _logger =
            logger;
    }

    public Task HandleAsync(
        StockDepletedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            integrationEvent);

        _logger.LogInformation(
            "Stock depleted integration event received. " +
            "EventId: {EventId}, ProductId: {ProductId}, OccurredAtUtc: {OccurredAtUtc}",
            integrationEvent.EventId,
            integrationEvent.ProductId,
            integrationEvent.OccurredAtUtc);

        return Task.CompletedTask;
    }
}