using System.Text.Json;
using Confluent.Kafka;
using ECommerce.FlashSaleOrchestrator.Application.Abstractions.Messaging;
using ECommerce.FlashSaleOrchestrator.Application.IntegrationEvents.Inventory;
using ECommerce.FlashSaleOrchestrator.Infrastructure.IntegrationTests.Outbox;
using ECommerce.FlashSaleOrchestrator.Worker.BackgroundServices;
using ECommerce.FlashSaleOrchestrator.Worker.Messaging.Kafka;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ECommerce.FlashSaleOrchestrator.Infrastructure.IntegrationTests.Kafka;

public sealed class StockDepletedConsumerIntegrationTests
{
    private static readonly JsonSerializerOptions SerializerOptions =
        new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task ConsumerWorker_ShouldDispatchEventAndCommitOffset_WhenHandlerSucceeds()
    {
        await using var topic =
            await KafkaTestTopic.CreateAsync();

        var consumerGroupId =
            $"flashsale-consumer-tests-{Guid.NewGuid():N}";

        var handler =
            new RecordingIntegrationEventHandler();

        var options =
            Options.Create(
                new KafkaConsumerOptions
                {
                    BootstrapServers =
                        topic.BootstrapServers,

                    StockDepletedTopic =
                        topic.Name,

                    ConsumerGroupId =
                        consumerGroupId
                });

        using var worker =
            new StockDepletedConsumerWorker(
                options,
                handler,
                NullLogger<
                    StockDepletedConsumerWorker>.Instance);

        await worker.StartAsync(
            CancellationToken.None);

        try
        {
            var integrationEvent =
                new StockDepletedIntegrationEvent(
                    Guid.NewGuid(),
                    DateTime.UtcNow,
                    Guid.NewGuid());

            using var producer =
                new ProducerBuilder<string, string>(
                    new ProducerConfig
                    {
                        BootstrapServers =
                            topic.BootstrapServers,

                        Acks =
                            Acks.All
                    })
                    .Build();

            var payload =
                JsonSerializer.Serialize(
                    integrationEvent,
                    SerializerOptions);

            await producer.ProduceAsync(
                topic.Name,
                new Message<string, string>
                {
                    Key =
                        integrationEvent.EventId.ToString(
                            "D"),

                    Value =
                        payload
                });

            var consumedEvent =
                await handler.WaitAsync(
                    TimeSpan.FromSeconds(15));

            Assert.Equal(
                integrationEvent.EventId,
                consumedEvent.EventId);

            Assert.Equal(
                integrationEvent.ProductId,
                consumedEvent.ProductId);

            Assert.Equal(
                integrationEvent.OccurredAtUtc,
                consumedEvent.OccurredAtUtc);
        }
        finally
        {
            await worker.StopAsync(
                CancellationToken.None);
        }

        using var verificationConsumer =
            new ConsumerBuilder<string, string>(
                new ConsumerConfig
                {
                    BootstrapServers =
                        topic.BootstrapServers,

                    GroupId =
                        consumerGroupId,

                    AutoOffsetReset =
                        AutoOffsetReset.Earliest,

                    EnableAutoCommit =
                        false
                })
                .Build();

        verificationConsumer.Subscribe(
            topic.Name);

        var replayedMessage =
            verificationConsumer.Consume(
                TimeSpan.FromSeconds(5));

        Assert.Null(
            replayedMessage);

        verificationConsumer.Close();
    }

    private sealed class RecordingIntegrationEventHandler
        : IIntegrationEventHandler<
            StockDepletedIntegrationEvent>
    {
        private readonly TaskCompletionSource<
            StockDepletedIntegrationEvent> _messageReceived =
                new(
                    TaskCreationOptions
                        .RunContinuationsAsynchronously);

        public Task HandleAsync(
            StockDepletedIntegrationEvent integrationEvent,
            CancellationToken cancellationToken = default)
        {
            _messageReceived.TrySetResult(
                integrationEvent);

            return Task.CompletedTask;
        }

        public Task<StockDepletedIntegrationEvent> WaitAsync(
            TimeSpan timeout)
        {
            return _messageReceived
                .Task
                .WaitAsync(
                    timeout);
        }
    }
}