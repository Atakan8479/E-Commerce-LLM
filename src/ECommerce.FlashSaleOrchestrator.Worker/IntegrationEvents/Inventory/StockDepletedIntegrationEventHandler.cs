using System.Diagnostics;
using ECommerce.FlashSaleOrchestrator.Application
    .Abstractions.Messaging;
using ECommerce.FlashSaleOrchestrator.Application
    .AlternativeRecommendations;
using ECommerce.FlashSaleOrchestrator.Application
    .IntegrationEvents.Inventory;
using ECommerce.FlashSaleOrchestrator.Worker
    .Observability;

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

        var startedAt =
            Stopwatch.GetTimestamp();

        _logger.LogInformation(
            "Stock depleted integration event received. " +
            "EventId: {EventId}, " +
            "ProductId: {ProductId}, " +
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
            WorkerMetrics.StockDepletedProcessed.Add(
                1,
                new KeyValuePair<string, object?>(
                    "outcome",
                    "product_unresolved"));

            var durationMs =
                Stopwatch.GetElapsedTime(
                        startedAt)
                    .TotalMilliseconds;

            _logger.LogWarning(
                "Recommendation plan was not created because " +
                "the depleted product could not be resolved. " +
                "EventId: {EventId}, " +
                "ProductId: {ProductId}, " +
                "CorrelationId: {CorrelationId}, " +
                "DurationMs: {DurationMs}",
                integrationEvent.EventId,
                integrationEvent.ProductId,
                integrationEvent.CorrelationId,
                durationMs);

            return;
        }

        var source =
            NormalizeRecommendationSource(
                plan.Source);

        WorkerMetrics.StockDepletedProcessed.Add(
            1,
            new KeyValuePair<string, object?>(
                "outcome",
                "plan_created"));

        WorkerMetrics.RecommendationPlansGenerated.Add(
            1,
            new KeyValuePair<string, object?>(
                "source",
                source));

        var completedDurationMs =
            Stopwatch.GetElapsedTime(
                    startedAt)
                .TotalMilliseconds;

        _logger.LogInformation(
            "Recommendation plan created. " +
            "EventId: {EventId}, " +
            "ProductId: {ProductId}, " +
            "CorrelationId: {CorrelationId}, " +
            "Source: {Source}, " +
            "RecommendationCount: {RecommendationCount}, " +
            "DurationMs: {DurationMs}",
            plan.EventId,
            plan.OriginalProductId,
            plan.CorrelationId,
            plan.Source,
            plan.Result.Recommendations.Count,
            completedDurationMs);
    }

    private static string NormalizeRecommendationSource(
        AlternativeRecommendationSource source)
    {
        return source switch
        {
            AlternativeRecommendationSource.Cache =>
                "cache",

            AlternativeRecommendationSource.Llm =>
                "llm",

            AlternativeRecommendationSource.Deterministic =>
                "deterministic",

            _ =>
                "unknown"
        };
    }
}