using ECommerce.FlashSaleOrchestrator.Application.Abstractions.AlternativeCandidates;
using ECommerce.FlashSaleOrchestrator.Application.Abstractions.Messaging;
using ECommerce.FlashSaleOrchestrator.Application.IntegrationEvents.Inventory;

namespace ECommerce.FlashSaleOrchestrator.Worker.IntegrationEvents.Inventory;

public sealed class StockDepletedIntegrationEventHandler
    : IIntegrationEventHandler<StockDepletedIntegrationEvent>
{
    private const int AlternativeCandidateLimit = 10;

    private readonly IAlternativeCandidateProvider
        _alternativeCandidateProvider;

    private readonly ILogger<StockDepletedIntegrationEventHandler>
        _logger;

    public StockDepletedIntegrationEventHandler(
        IAlternativeCandidateProvider alternativeCandidateProvider,
        ILogger<StockDepletedIntegrationEventHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(
            alternativeCandidateProvider);

        ArgumentNullException.ThrowIfNull(
            logger);

        _alternativeCandidateProvider =
            alternativeCandidateProvider;

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
            "CorrelationId: {CorrelationId}, OccurredAtUtc: {OccurredAtUtc}",
            integrationEvent.EventId,
            integrationEvent.ProductId,
            integrationEvent.CorrelationId,
            integrationEvent.OccurredAtUtc);

        var candidates =
            await _alternativeCandidateProvider
                .GetCandidatesAsync(
                    integrationEvent.ProductId,
                    AlternativeCandidateLimit,
                    cancellationToken);

        if (candidates.Count == 0)
        {
            _logger.LogInformation(
                "No eligible alternative candidates found. " +
                "EventId: {EventId}, ProductId: {ProductId}, " +
                "CorrelationId: {CorrelationId}",
                integrationEvent.EventId,
                integrationEvent.ProductId,
                integrationEvent.CorrelationId);

            return;
        }

        _logger.LogInformation(
            "Alternative candidates retrieved. " +
            "EventId: {EventId}, ProductId: {ProductId}, " +
            "CorrelationId: {CorrelationId}, CandidateCount: {CandidateCount}",
            integrationEvent.EventId,
            integrationEvent.ProductId,
            integrationEvent.CorrelationId,
            candidates.Count);
    }
}