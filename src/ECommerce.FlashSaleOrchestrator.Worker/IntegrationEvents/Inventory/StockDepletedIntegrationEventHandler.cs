using ECommerce.FlashSaleOrchestrator.Application.Abstractions.AlternativeCandidates;
using ECommerce.FlashSaleOrchestrator.Application.Abstractions.Messaging;
using ECommerce.FlashSaleOrchestrator.Application.AlternativeRecommendations;
using ECommerce.FlashSaleOrchestrator.Application.IntegrationEvents.Inventory;

namespace ECommerce.FlashSaleOrchestrator.Worker.IntegrationEvents.Inventory;

public sealed class StockDepletedIntegrationEventHandler
    : IIntegrationEventHandler<StockDepletedIntegrationEvent>
{
    private const int AlternativeCandidateLimit = 10;

    private readonly IAlternativeCandidateProvider
        _alternativeCandidateProvider;

    private readonly IAlternativeRecommendationGenerator
        _alternativeRecommendationGenerator;

    private readonly ILogger<StockDepletedIntegrationEventHandler>
        _logger;

    public StockDepletedIntegrationEventHandler(
        IAlternativeCandidateProvider alternativeCandidateProvider,
        IAlternativeRecommendationGenerator alternativeRecommendationGenerator,
        ILogger<StockDepletedIntegrationEventHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(
            alternativeCandidateProvider);

        ArgumentNullException.ThrowIfNull(
            alternativeRecommendationGenerator);

        ArgumentNullException.ThrowIfNull(
            logger);

        _alternativeCandidateProvider =
            alternativeCandidateProvider;

        _alternativeRecommendationGenerator =
            alternativeRecommendationGenerator;

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

        var candidateSet =
            await _alternativeCandidateProvider
                .GetCandidateSetAsync(
                    integrationEvent.ProductId,
                    AlternativeCandidateLimit,
                    cancellationToken);

        if (candidateSet is null)
        {
            _logger.LogWarning(
                "Depleted product was not found while retrieving alternatives. " +
                "EventId: {EventId}, ProductId: {ProductId}, " +
                "CorrelationId: {CorrelationId}",
                integrationEvent.EventId,
                integrationEvent.ProductId,
                integrationEvent.CorrelationId);

            return;
        }

        if (candidateSet.Candidates.Count == 0)
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
            candidateSet.Candidates.Count);

        var request =
            new AlternativeRecommendationRequest(
                integrationEvent.CorrelationId,
                candidateSet.DepletedProduct,
                candidateSet.Candidates);

        var result =
            await _alternativeRecommendationGenerator
                .GenerateAsync(
                    request,
                    cancellationToken);

        _logger.LogInformation(
            "Alternative recommendation generation completed. " +
            "EventId: {EventId}, ProductId: {ProductId}, " +
            "CorrelationId: {CorrelationId}, RecommendationCount: {RecommendationCount}",
            integrationEvent.EventId,
            integrationEvent.ProductId,
            integrationEvent.CorrelationId,
            result.Recommendations.Count);
    }
}