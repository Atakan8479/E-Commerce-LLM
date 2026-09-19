using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using ECommerce.FlashSaleOrchestrator.Application
    .AlternativeRecommendations;
using ECommerce.FlashSaleOrchestrator.Application
    .IntegrationEvents.Inventory;
using ECommerce.FlashSaleOrchestrator.Worker
    .IntegrationEvents.Inventory;
using Microsoft.Extensions.Logging.Abstractions;

namespace ECommerce.FlashSaleOrchestrator.Worker.Tests
    .IntegrationEvents.Inventory;

public sealed class
    StockDepletedIntegrationEventHandlerMetricsTests
{
    [Fact]
    public async Task
        HandleAsync_ShouldEmitProcessedAndGeneratedMetrics_WhenPlanIsCreated()
    {
        var integrationEvent =
            new StockDepletedIntegrationEvent(
                Guid.NewGuid(),
                DateTime.UtcNow,
                Guid.NewGuid(),
                $"metrics-correlation-{Guid.NewGuid():N}");

        var plan =
            new AlternativeRecommendationPlan(
                integrationEvent.EventId,
                integrationEvent.ProductId,
                new AlternativeRecommendationResult(
                    []),
                AlternativeRecommendationSource.Cache,
                integrationEvent.CorrelationId,
                DateTime.UtcNow);

        var orchestrator =
            new FakeStockDepletedRecommendationOrchestrator(
                plan);

        var measurements =
            new ConcurrentQueue<MetricMeasurement>();

        using var listener =
            CreateMetricsListener(
                measurements);

        var handler =
            new StockDepletedIntegrationEventHandler(
                orchestrator,
                NullLogger<
                    StockDepletedIntegrationEventHandler>
                    .Instance);

        await handler.HandleAsync(
            integrationEvent);

        Assert.Contains(
            measurements,
            measurement =>
                measurement.InstrumentName ==
                    "flashsale.stock_depleted.processed"
                && measurement.Value == 1
                && measurement.Outcome ==
                    "plan_created");

        Assert.Contains(
            measurements,
            measurement =>
                measurement.InstrumentName ==
                    "flashsale.recommendation_plans.generated"
                && measurement.Value == 1
                && measurement.Source ==
                    "cache");
    }

    private static MeterListener
        CreateMetricsListener(
            ConcurrentQueue<MetricMeasurement> measurements)
    {
        ArgumentNullException.ThrowIfNull(
            measurements);

        var listener =
            new MeterListener();

        listener.InstrumentPublished =
            (instrument, meterListener) =>
            {
                if (instrument.Meter.Name !=
                    "ECommerce.FlashSaleOrchestrator.Worker")
                {
                    return;
                }

                meterListener.EnableMeasurementEvents(
                    instrument);
            };

        listener.SetMeasurementEventCallback<long>(
            (
                instrument,
                measurement,
                tags,
                _) =>
            {
                string? outcome =
                    null;

                string? source =
                    null;

                foreach (var tag in tags)
                {
                    if (string.Equals(
                            tag.Key,
                            "outcome",
                            StringComparison.Ordinal))
                    {
                        outcome =
                            tag.Value?
                                .ToString();

                        continue;
                    }

                    if (string.Equals(
                            tag.Key,
                            "source",
                            StringComparison.Ordinal))
                    {
                        source =
                            tag.Value?
                                .ToString();
                    }
                }

                measurements.Enqueue(
                    new MetricMeasurement(
                        instrument.Name,
                        measurement,
                        outcome,
                        source));
            });

        listener.Start();

        return listener;
    }

    private sealed record MetricMeasurement(
        string InstrumentName,
        long Value,
        string? Outcome,
        string? Source);

    private sealed class
        FakeStockDepletedRecommendationOrchestrator
        : IStockDepletedRecommendationOrchestrator
    {
        private readonly AlternativeRecommendationPlan
            _plan;

        public FakeStockDepletedRecommendationOrchestrator(
            AlternativeRecommendationPlan plan)
        {
            ArgumentNullException.ThrowIfNull(
                plan);

            _plan =
                plan;
        }

        public Task<AlternativeRecommendationPlan?>
            OrchestrateAsync(
                Guid eventId,
                Guid depletedProductId,
                string correlationId,
                CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult<
                AlternativeRecommendationPlan?>(
                    _plan);
        }
    }
}