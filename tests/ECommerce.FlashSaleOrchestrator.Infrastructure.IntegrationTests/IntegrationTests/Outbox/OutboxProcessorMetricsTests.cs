using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using System.Text.Json;
using ECommerce.FlashSaleOrchestrator.Application
    .Abstractions.Messaging;
using ECommerce.FlashSaleOrchestrator.Domain
    .Inventory.Events;
using ECommerce.FlashSaleOrchestrator.Infrastructure
    .Persistence.Outbox;
using Microsoft.Extensions.Logging.Abstractions;

namespace ECommerce.FlashSaleOrchestrator.Infrastructure
    .IntegrationTests.Outbox;

public sealed class OutboxProcessorMetricsTests
{
    [Fact]
    public async Task
        ProcessPendingAsync_ShouldEmitPendingAndPublishedMetrics_WhenMessageIsProcessed()
    {
        await using var database =
            await OutboxTestDatabase.CreateAsync();

        await SeedPendingMessageAsync(
            database);

        var measurements =
            new ConcurrentQueue<MetricMeasurement>();

        using var listener =
            CreateMetricsListener(
                measurements);

        await using var context =
            database.CreateContext();

        var publisher =
            new RecordingEventPublisher();

        var processor =
            new OutboxProcessor(
                context,
                new StockDepletedOutboxMessageMapper(),
                publisher,
                NullLogger<OutboxProcessor>.Instance);

        var processedCount =
            await processor.ProcessPendingAsync(
                10);

        Assert.Equal(
            1,
            processedCount);

        Assert.Equal(
            1,
            publisher.CallCount);

        Assert.Contains(
            measurements,
            measurement =>
                measurement.InstrumentName ==
                    "flashsale.outbox.pending"
                && measurement.Value == 1);

        Assert.Contains(
            measurements,
            measurement =>
                measurement.InstrumentName ==
                    "flashsale.outbox.pending"
                && measurement.Value == 0);

        Assert.Contains(
            measurements,
            measurement =>
                measurement.InstrumentName ==
                    "flashsale.outbox.published"
                && measurement.Value == 1);
    }

    [Fact]
    public async Task
        ProcessPendingAsync_ShouldEmitPublishFailureMetric_WhenPublisherFails()
    {
        await using var database =
            await OutboxTestDatabase.CreateAsync();

        await SeedPendingMessageAsync(
            database);

        var measurements =
            new ConcurrentQueue<MetricMeasurement>();

        using var listener =
            CreateMetricsListener(
                measurements);

        await using var context =
            database.CreateContext();

        var processor =
            new OutboxProcessor(
                context,
                new StockDepletedOutboxMessageMapper(),
                new ThrowingEventPublisher(),
                NullLogger<OutboxProcessor>.Instance);

        await Assert.ThrowsAsync<
            InvalidOperationException>(
            () =>
                processor.ProcessPendingAsync(
                    10));

        Assert.Contains(
            measurements,
            measurement =>
                measurement.InstrumentName ==
                    "flashsale.outbox.pending"
                && measurement.Value == 1);

        Assert.Contains(
            measurements,
            measurement =>
                measurement.InstrumentName ==
                    "flashsale.outbox.failures"
                && measurement.Value == 1
                && measurement.Stage ==
                    "publish");
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
                    "ECommerce.FlashSaleOrchestrator.Infrastructure")
                {
                    return;
                }

                if (!instrument.Name.StartsWith(
                        "flashsale.outbox.",
                        StringComparison.Ordinal))
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
                string? stage =
                    null;

                foreach (var tag in tags)
                {
                    if (!string.Equals(
                            tag.Key,
                            "stage",
                            StringComparison.Ordinal))
                    {
                        continue;
                    }

                    stage =
                        tag.Value?
                            .ToString();

                    break;
                }

                measurements.Enqueue(
                    new MetricMeasurement(
                        instrument.Name,
                        measurement,
                        stage));
            });

        listener.Start();

        return listener;
    }

    private static async Task SeedPendingMessageAsync(
        OutboxTestDatabase database)
    {
        var productId =
            Guid.NewGuid();

        var payload =
            JsonSerializer.Serialize(
                new
                {
                    ProductId =
                        new
                        {
                            Value =
                                productId
                        }
                });

        var eventType =
            typeof(
                StockDepletedDomainEvent)
                .FullName
            ?? nameof(
                StockDepletedDomainEvent);

        var outboxMessage =
            new OutboxMessage(
                Guid.NewGuid(),
                DateTime.UtcNow,
                eventType,
                payload,
                $"outbox-metrics-{Guid.NewGuid():N}");

        await using var context =
            database.CreateContext();

        context.OutboxMessages.Add(
            outboxMessage);

        await context.SaveChangesAsync();
    }

    private sealed record MetricMeasurement(
        string InstrumentName,
        long Value,
        string? Stage);

    private sealed class RecordingEventPublisher
        : IEventPublisher
    {
        public int CallCount { get; private set; }

        public Task PublishAsync<TEvent>(
            TEvent integrationEvent,
            CancellationToken cancellationToken = default)
            where TEvent : class
        {
            ArgumentNullException.ThrowIfNull(
                integrationEvent);

            cancellationToken.ThrowIfCancellationRequested();

            CallCount++;

            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingEventPublisher
        : IEventPublisher
    {
        public Task PublishAsync<TEvent>(
            TEvent integrationEvent,
            CancellationToken cancellationToken = default)
            where TEvent : class
        {
            ArgumentNullException.ThrowIfNull(
                integrationEvent);

            cancellationToken.ThrowIfCancellationRequested();

            throw new InvalidOperationException(
                "Simulated broker failure.");
        }
    }
}