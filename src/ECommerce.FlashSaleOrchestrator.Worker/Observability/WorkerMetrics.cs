using System.Diagnostics.Metrics;

namespace ECommerce.FlashSaleOrchestrator.Worker
    .Observability;

internal static class WorkerMetrics
{
    internal const string MeterName =
        "ECommerce.FlashSaleOrchestrator.Worker";

    private static readonly Meter Meter =
        new(
            MeterName);

    internal static readonly Counter<long>
        ConsumerRetries =
            Meter.CreateCounter<long>(
                "flashsale.consumer.retries");

    internal static readonly Counter<long>
        ConsumerDeadLetters =
            Meter.CreateCounter<long>(
                "flashsale.consumer.dead_letters");

    internal static readonly Counter<long>
        StockDepletedProcessed =
            Meter.CreateCounter<long>(
                "flashsale.stock_depleted.processed");

    internal static readonly Counter<long>
        RecommendationPlansGenerated =
            Meter.CreateCounter<long>(
                "flashsale.recommendation_plans.generated");
}