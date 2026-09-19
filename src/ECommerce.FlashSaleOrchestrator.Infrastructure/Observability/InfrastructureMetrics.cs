using System.Diagnostics.Metrics;

namespace ECommerce.FlashSaleOrchestrator.Infrastructure
    .Observability;

internal static class InfrastructureMetrics
{
    internal const string MeterName =
        "ECommerce.FlashSaleOrchestrator.Infrastructure";

    private static readonly Meter Meter =
        new(
            MeterName);

    internal static readonly Counter<long>
        SemanticCacheLookups =
            Meter.CreateCounter<long>(
                "flashsale.semantic_cache.lookups");

    internal static readonly Counter<long>
        LlmRequests =
            Meter.CreateCounter<long>(
                "flashsale.llm.requests");

    internal static readonly Counter<long>
        LlmFailures =
            Meter.CreateCounter<long>(
                "flashsale.llm.failures");

    internal static readonly Histogram<double>
        LlmDuration =
            Meter.CreateHistogram<double>(
                "flashsale.llm.duration",
                unit: "ms");

    internal static readonly Gauge<long>
        OutboxPending =
            Meter.CreateGauge<long>(
                "flashsale.outbox.pending");

    internal static readonly Counter<long>
        OutboxPublished =
            Meter.CreateCounter<long>(
                "flashsale.outbox.published");

    internal static readonly Counter<long>
        OutboxFailures =
            Meter.CreateCounter<long>(
                "flashsale.outbox.failures");
}