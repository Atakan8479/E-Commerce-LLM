using System.Text.Json;
using Confluent.Kafka;
using ECommerce.FlashSaleOrchestrator.Application.Abstractions.Messaging;
using ECommerce.FlashSaleOrchestrator.Application.IntegrationEvents.Inventory;
using ECommerce.FlashSaleOrchestrator.Worker.BackgroundServices;
using ECommerce.FlashSaleOrchestrator.Worker.Messaging.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ECommerce.FlashSaleOrchestrator.Infrastructure.IntegrationTests.Kafka;

public sealed class StockDepletedConsumerIntegrationTests
{
    private static readonly JsonSerializerOptions SerializerOptions =
        new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task ConsumerWorker_ShouldDispatchEventAndCommitOffset_WhenProcessorSucceeds()
    {
        await using var topic =
            await KafkaTestTopic.CreateAsync();

        var consumerGroupId =
            $"flashsale-consumer-tests-{Guid.NewGuid():N}";

        var processor =
            new RecordingIntegrationEventProcessor();

        var services =
            new ServiceCollection();

        services.AddScoped<
            IIntegrationEventProcessor<
                StockDepletedIntegrationEvent>>(
            _ => processor);

        await using var serviceProvider =
            services.BuildServiceProvider();

        var serviceScopeFactory =
            serviceProvider.GetRequiredService<
                IServiceScopeFactory>();

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
                serviceScopeFactory,
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

            var processedEvent =
                await processor.WaitAsync(
                    TimeSpan.FromSeconds(15));

            Assert.Equal(
                integrationEvent.EventId,
                processedEvent.EventId);

            Assert.Equal(
                integrationEvent.ProductId,
                processedEvent.ProductId);

            Assert.Equal(
                integrationEvent.OccurredAtUtc,
                processedEvent.OccurredAtUtc);
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

    private sealed class RecordingIntegrationEventProcessor
        : IIntegrationEventProcessor<
            StockDepletedIntegrationEvent>
    {
        private readonly TaskCompletionSource<
            StockDepletedIntegrationEvent> _messageReceived =
                new(
                    TaskCreationOptions
                        .RunContinuationsAsynchronously);

        public Task<IntegrationEventProcessingResult> ProcessAsync(
            StockDepletedIntegrationEvent integrationEvent,
            CancellationToken cancellationToken = default)
        {
            _messageReceived.TrySetResult(
                integrationEvent);

            return Task.FromResult(
                IntegrationEventProcessingResult.Processed);
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