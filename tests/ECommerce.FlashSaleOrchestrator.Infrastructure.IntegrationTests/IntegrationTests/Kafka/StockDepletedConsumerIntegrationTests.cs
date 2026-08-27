using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using ECommerce.FlashSaleOrchestrator.Application.Abstractions.Messaging;
using ECommerce.FlashSaleOrchestrator.Application.Abstractions.Observability;
using ECommerce.FlashSaleOrchestrator.Application.IntegrationEvents.Inventory;
using ECommerce.FlashSaleOrchestrator.Infrastructure.Observability;
using ECommerce.FlashSaleOrchestrator.Worker.BackgroundServices;
using ECommerce.FlashSaleOrchestrator.Worker.Messaging.DeadLetter;
using ECommerce.FlashSaleOrchestrator.Worker.Messaging.Kafka;
using ECommerce.FlashSaleOrchestrator.Worker.Resilience;
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

        var processingProbe =
            new ProcessingProbe();

        var services =
            new ServiceCollection();

        services.AddSingleton(
            processingProbe);

        services.AddScoped<
            ICorrelationContext,
            CorrelationContext>();

        services.AddScoped<
            IIntegrationEventProcessor<
                StockDepletedIntegrationEvent>,
            RecordingIntegrationEventProcessor>();

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

                    StockDepletedDeadLetterTopic =
                        $"{topic.Name}.dlq",

                    ConsumerGroupId =
                        consumerGroupId
                });

        var retryExecutor =
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
                    IntegrationEventRetryExecutor>.Instance);

        var deadLetterPublisher =
            new RecordingDeadLetterPublisher();

        using var worker =
            new StockDepletedConsumerWorker(
                options,
                serviceScopeFactory,
                retryExecutor,
                deadLetterPublisher,
                NullLogger<
                    StockDepletedConsumerWorker>.Instance);

        await worker.StartAsync(
            CancellationToken.None);

        try
        {
            const string correlationId =
                "consumer-correlation-123";

            var integrationEvent =
                new StockDepletedIntegrationEvent(
                    Guid.NewGuid(),
                    DateTime.UtcNow,
                    Guid.NewGuid(),
                    correlationId);

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

            var headers =
                new Headers();

            headers.Add(
                CorrelationMetadata.HeaderName,
                Encoding.UTF8.GetBytes(
                    correlationId));

            await producer.ProduceAsync(
                topic.Name,
                new Message<string, string>
                {
                    Key =
                        integrationEvent.EventId.ToString(
                            "D"),

                    Value =
                        payload,

                    Headers =
                        headers
                });

            var processedMessage =
                await processingProbe.WaitAsync(
                    TimeSpan.FromSeconds(15));

            Assert.Equal(
                integrationEvent.EventId,
                processedMessage.IntegrationEvent.EventId);

            Assert.Equal(
                integrationEvent.ProductId,
                processedMessage.IntegrationEvent.ProductId);

            Assert.Equal(
                integrationEvent.OccurredAtUtc,
                processedMessage.IntegrationEvent.OccurredAtUtc);

            Assert.Equal(
                correlationId,
                processedMessage.IntegrationEvent.CorrelationId);

            Assert.Equal(
                correlationId,
                processedMessage.ScopedCorrelationId);
        }
        finally
        {
            await worker.StopAsync(
                CancellationToken.None);
        }

        Assert.Equal(
            0,
            deadLetterPublisher.InvocationCount);

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
        private readonly ICorrelationContext
            _correlationContext;

        private readonly ProcessingProbe
            _processingProbe;

        public RecordingIntegrationEventProcessor(
            ICorrelationContext correlationContext,
            ProcessingProbe processingProbe)
        {
            ArgumentNullException.ThrowIfNull(
                correlationContext);

            ArgumentNullException.ThrowIfNull(
                processingProbe);

            _correlationContext =
                correlationContext;

            _processingProbe =
                processingProbe;
        }

        public Task<IntegrationEventProcessingResult> ProcessAsync(
            StockDepletedIntegrationEvent integrationEvent,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(
                integrationEvent);

            _processingProbe.Record(
                integrationEvent,
                _correlationContext.CorrelationId);

            return Task.FromResult(
                IntegrationEventProcessingResult.Processed);
        }
    }

    private sealed class ProcessingProbe
    {
        private readonly TaskCompletionSource<
            ProcessedMessage> _messageReceived =
                new(
                    TaskCreationOptions
                        .RunContinuationsAsynchronously);

        public void Record(
            StockDepletedIntegrationEvent integrationEvent,
            string scopedCorrelationId)
        {
            _messageReceived.TrySetResult(
                new ProcessedMessage(
                    integrationEvent,
                    scopedCorrelationId));
        }

        public Task<ProcessedMessage> WaitAsync(
            TimeSpan timeout)
        {
            return _messageReceived
                .Task
                .WaitAsync(
                    timeout);
        }
    }

    private sealed record ProcessedMessage(
        StockDepletedIntegrationEvent IntegrationEvent,
        string ScopedCorrelationId);

    private sealed class RecordingDeadLetterPublisher
        : IDeadLetterPublisher
    {
        public int InvocationCount { get; private set; }

        public Task PublishAsync(
            DeadLetterMessage message,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(
                message);

            InvocationCount++;

            return Task.CompletedTask;
        }
    }
}