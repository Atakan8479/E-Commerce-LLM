using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using ECommerce.FlashSaleOrchestrator.Infrastructure
    .IntegrationTests.Kafka;
using ECommerce.FlashSaleOrchestrator.Worker
    .Messaging.DeadLetter;
using ECommerce.FlashSaleOrchestrator.Worker
    .Messaging.Kafka;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ECommerce.FlashSaleOrchestrator.Infrastructure
    .IntegrationTests.Kafka;

public sealed class KafkaDeadLetterPublisherMetricsTests
{
    [Fact]
    public async Task
        PublishAsync_ShouldEmitDeadLetterMetric_WhenKafkaPublishSucceeds()
    {
        await using var topic =
            await KafkaTestTopic.CreateAsync();

        var measurements =
            new ConcurrentQueue<long>();

        using var listener =
            CreateMetricsListener(
                measurements);

        using var publisher =
            new KafkaDeadLetterPublisher(
                Options.Create(
                    new KafkaConsumerOptions
                    {
                        BootstrapServers =
                            topic.BootstrapServers,

                        StockDepletedTopic =
                            topic.Name,

                        StockDepletedDeadLetterTopic =
                            topic.Name,

                        ConsumerGroupId =
                            $"dead-letter-metrics-{Guid.NewGuid():N}"
                    }),
                NullLogger<
                    KafkaDeadLetterPublisher>.Instance);

        var message =
            new DeadLetterMessage(
                Guid.NewGuid(),
                "stock-depleted",
                "dead-letter-metrics-correlation",
                Guid.NewGuid().ToString("D"),
                "inventory.stock-depleted",
                0,
                10,
                "{\"productId\":\"test\"}",
                typeof(InvalidOperationException)
                    .FullName!,
                "Simulated processing failure.",
                DateTime.UtcNow);

        await publisher.PublishAsync(
            message);

        var emittedMeasurements =
            measurements.ToArray();

        Assert.Single(
            emittedMeasurements);

        Assert.Equal(
            1,
            emittedMeasurements[0]);
    }

    private static MeterListener
        CreateMetricsListener(
            ConcurrentQueue<long> measurements)
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

                if (instrument.Name !=
                    "flashsale.consumer.dead_letters")
                {
                    return;
                }

                meterListener.EnableMeasurementEvents(
                    instrument);
            };

        listener.SetMeasurementEventCallback<long>(
            (
                _,
                measurement,
                _,
                _) =>
            {
                measurements.Enqueue(
                    measurement);
            });

        listener.Start();

        return listener;
    }
}