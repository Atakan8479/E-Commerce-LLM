using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using ECommerce.FlashSaleOrchestrator.Worker.Resilience;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ECommerce.FlashSaleOrchestrator.Worker.Tests.Resilience;

public sealed class IntegrationEventRetryExecutorMetricsTests
{
    [Fact]
    public async Task
        ExecuteAsync_ShouldEmitRetryMetricForEachScheduledRetry()
    {
        const string eventType =
            "metrics-test-stock-depleted";

        var measurements =
            new ConcurrentQueue<RetryMeasurement>();

        using var listener =
            CreateMetricsListener(
                measurements);

        var executor =
            new IntegrationEventRetryExecutor(
                Options.Create(
                    new EventProcessingRetryOptions
                    {
                        MaxAttempts =
                            3,

                        InitialDelay =
                            TimeSpan.Zero
                    }),
                NullLogger<
                    IntegrationEventRetryExecutor>
                    .Instance);

        var invocationCount =
            0;

        await Assert.ThrowsAsync<
            InvalidOperationException>(
            () =>
                executor.ExecuteAsync<int>(
                    _ =>
                    {
                        invocationCount++;

                        throw new InvalidOperationException(
                            "Transient processing failure.");
                    },
                    Guid.NewGuid(),
                    eventType));

        Assert.Equal(
            3,
            invocationCount);

        var retryMeasurements =
            measurements
                .Where(
                    measurement =>
                        measurement.InstrumentName ==
                            "flashsale.consumer.retries"
                        && measurement.EventType ==
                            eventType)
                .ToArray();

        Assert.Equal(
            2,
            retryMeasurements.Length);

        Assert.All(
            retryMeasurements,
            measurement =>
                Assert.Equal(
                    1,
                    measurement.Value));
    }

    private static MeterListener
        CreateMetricsListener(
            ConcurrentQueue<RetryMeasurement> measurements)
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
                string? eventType =
                    null;

                foreach (var tag in tags)
                {
                    if (!string.Equals(
                            tag.Key,
                            "event_type",
                            StringComparison.Ordinal))
                    {
                        continue;
                    }

                    eventType =
                        tag.Value?
                            .ToString();

                    break;
                }

                measurements.Enqueue(
                    new RetryMeasurement(
                        instrument.Name,
                        measurement,
                        eventType));
            });

        listener.Start();

        return listener;
    }

    private sealed record RetryMeasurement(
        string InstrumentName,
        long Value,
        string? EventType);
}