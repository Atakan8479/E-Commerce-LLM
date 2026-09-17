using ECommerce.FlashSaleOrchestrator.Application
    .Abstractions.Messaging;
using ECommerce.FlashSaleOrchestrator.Application
    .AlternativeRecommendations;
using ECommerce.FlashSaleOrchestrator.Application
    .IntegrationEvents.Inventory;

namespace ECommerce.FlashSaleOrchestrator.Worker
    .IntegrationEvents.Inventory;

public sealed class StockDepletedIntegrationEventHandler
    : IIntegrationEventHandler<StockDepletedIntegrationEvent>
{
    private readonly IStockDepletedRecommendationOrchestrator
        _orchestrator;

    private readonly ILogger<
        StockDepletedIntegrationEventHandler>
        _logger;

    public StockDepletedIntegrationEventHandler(
        IStockDepletedRecommendationOrchestrator orchestrator,
        ILogger<StockDepletedIntegrationEventHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(
            orchestrator);

        ArgumentNullException.ThrowIfNull(
            logger);

        _orchestrator =
            orchestrator;

        _logger =
            logger;
    }

    public async Task HandleAsync(
        StockDepletedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            integrationEvent);

        _logger.LogInformation(
            "Stock depleted integration event received. " +
            "EventId: {EventId}, ProductId: {ProductId}, " +
            "CorrelationId: {CorrelationId}, " +
            "OccurredAtUtc: {OccurredAtUtc}",
            integrationEvent.EventId,
            integrationEvent.ProductId,
            integrationEvent.CorrelationId,
            integrationEvent.OccurredAtUtc);

        var plan =
            await _orchestrator.OrchestrateAsync(
                integrationEvent.EventId,
                integrationEvent.ProductId,
                integrationEvent.CorrelationId,
                cancellationToken);

        if (plan is null)
        {
            _logger.LogWarning(
                "Recommendation plan was not created because " +
                "the depleted product could not be resolved. " +
                "EventId: {EventId}, ProductId: {ProductId}, " +
                "CorrelationId: {CorrelationId}",
                integrationEvent.EventId,
                integrationEvent.ProductId,
                integrationEvent.CorrelationId);

            return;
        }

        _logger.LogInformation(
            "Recommendation plan created. " +
            "EventId: {EventId}, ProductId: {ProductId}, " +
            "CorrelationId: {CorrelationId}, " +
            "Source: {Source}, " +
            "RecommendationCount: {RecommendationCount}",
            plan.EventId,
            plan.OriginalProductId,
            plan.CorrelationId,
            plan.Source,
            plan.Result.Recommendations.Count);
    }
}