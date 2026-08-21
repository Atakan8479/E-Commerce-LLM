using System.Text.Json;
using Confluent.Kafka;
using ECommerce.FlashSaleOrchestrator.Application.Abstractions.Messaging;
using ECommerce.FlashSaleOrchestrator.Application.IntegrationEvents.Inventory;
using ECommerce.FlashSaleOrchestrator.Worker.BackgroundServices;
using ECommerce.FlashSaleOrchestrator.Worker.Messaging.DeadLetter;
using ECommerce.FlashSaleOrchestrator.Worker.Messaging.Kafka;
using ECommerce.FlashSaleOrchestrator.Worker.Resilience;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ECommerce.FlashSaleOrchestrator.Infrastructure.IntegrationTests.Kafka;

public sealed class StockDepletedDeadLetterIntegrationTests
{
    private static readonly JsonSerializerOptions SerializerOptions =
        new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task ConsumerWorker_ShouldPublishToDeadLetterAndCommitOriginalOffset_WhenRetriesAreExhausted()
    {
        await using var sourceTopic =
            await KafkaTestTopic.CreateAsync();

        await using var deadLetterTopic =
            await KafkaTestTopic.CreateAsync();

        var consumerGroupId =
            $"flashsale-dlq-tests-{Guid.NewGuid():N}";

        var processor =
            new AlwaysFailingIntegrationEventProcessor();

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

        var kafkaOptions =
            Options.Create(
                new KafkaConsumerOptions
                {
                    BootstrapServers =
                        sourceTopic.BootstrapServers,

                    StockDepletedTopic =
                        sourceTopic.Name,

                    StockDepletedDeadLetterTopic =
                        deadLetterTopic.Name,

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

        using var deadLetterPublisher =
            new KafkaDeadLetterPublisher(
                kafkaOptions,
                NullLogger<
                    KafkaDeadLetterPublisher>.Instance);

        using var worker =
            new StockDepletedConsumerWorker(
                kafkaOptions,
                serviceScopeFactory,
                retryExecutor,
                deadLetterPublisher,
                NullLogger<
                    StockDepletedConsumerWorker>.Instance);

        using var deadLetterConsumer =
            new ConsumerBuilder<string, string>(
                new ConsumerConfig
                {
                    BootstrapServers =
                        deadLetterTopic.BootstrapServers,

                    GroupId =
                        $"flashsale-dlq-reader-{Guid.NewGuid():N}",

                    AutoOffsetReset =
                        AutoOffsetReset.Earliest,

                    EnableAutoCommit =
                        false
                })
                .Build();

        deadLetterConsumer.Subscribe(
            deadLetterTopic.Name);

        await worker.StartAsync(
            CancellationToken.None);

        StockDepletedIntegrationEvent integrationEvent;

        try
        {
            integrationEvent =
                new StockDepletedIntegrationEvent(
                    Guid.NewGuid(),
                    DateTime.UtcNow,
                    Guid.NewGuid());

            using var producer =
                new ProducerBuilder<string, string>(
                    new ProducerConfig
                    {
                        BootstrapServers =
                            sourceTopic.BootstrapServers,

                        Acks =
                            Acks.All
                    })
                    .Build();

            var originalPayload =
                JsonSerializer.Serialize(
                    integrationEvent,
                    SerializerOptions);

            await producer.ProduceAsync(
                sourceTopic.Name,
                new Message<string, string>
                {
                    Key =
                        integrationEvent.EventId.ToString(
                            "D"),

                    Value =
                        originalPayload
                });

            var deadLetterResult =
                deadLetterConsumer.Consume(
                    TimeSpan.FromSeconds(15));

            Assert.NotNull(
                deadLetterResult);

            Assert.Equal(
                integrationEvent.EventId.ToString(
                    "D"),
                deadLetterResult.Message.Key);

            var deadLetterMessage =
                JsonSerializer.Deserialize<
                    DeadLetterMessage>(
                        deadLetterResult.Message.Value,
                        SerializerOptions);

            Assert.NotNull(
                deadLetterMessage);

            Assert.True(
                deadLetterMessage.EventId.HasValue);

            Assert.Equal(
                integrationEvent.EventId,
                deadLetterMessage.EventId.Value);

            Assert.Equal(
                integrationEvent.EventType,
                deadLetterMessage.EventType);

            Assert.Equal(
                integrationEvent.EventId.ToString(
                    "D"),
                deadLetterMessage.OriginalKey);

            Assert.Equal(
                sourceTopic.Name,
                deadLetterMessage.OriginalTopic);

            Assert.Equal(
                0,
                deadLetterMessage.OriginalPartition);

            Assert.Equal(
                0,
                deadLetterMessage.OriginalOffset);

            Assert.Equal(
                originalPayload,
                deadLetterMessage.Payload);

            Assert.Contains(
                nameof(InvalidOperationException),
                deadLetterMessage.ErrorType);

            Assert.Equal(
                "Simulated permanent processing failure.",
                deadLetterMessage.ErrorMessage);

            Assert.Equal(
                DateTimeKind.Utc,
                deadLetterMessage.FailedAtUtc.Kind);

            Assert.True(
                deadLetterMessage.FailedAtUtc <=
                DateTime.UtcNow);

            Assert.Equal(
                3,
                processor.InvocationCount);

            await Task.Delay(
                TimeSpan.FromMilliseconds(250));
        }
        finally
        {
            await worker.StopAsync(
                CancellationToken.None);

            deadLetterConsumer.Close();
        }

        using var verificationConsumer =
            new ConsumerBuilder<string, string>(
                new ConsumerConfig
                {
                    BootstrapServers =
                        sourceTopic.BootstrapServers,

                    GroupId =
                        consumerGroupId,

                    AutoOffsetReset =
                        AutoOffsetReset.Earliest,

                    EnableAutoCommit =
                        false
                })
                .Build();

        verificationConsumer.Subscribe(
            sourceTopic.Name);

        var replayedMessage =
            verificationConsumer.Consume(
                TimeSpan.FromSeconds(5));

        Assert.Null(
            replayedMessage);

        verificationConsumer.Close();
    }

    [Fact]
    public async Task ConsumerWorker_ShouldNotCommitOriginalOffset_WhenDeadLetterPublishFails()
    {
        await using var sourceTopic =
            await KafkaTestTopic.CreateAsync();

        var consumerGroupId =
            $"flashsale-dlq-failure-tests-{Guid.NewGuid():N}";

        var processor =
            new AlwaysFailingIntegrationEventProcessor();

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

        var kafkaOptions =
            Options.Create(
                new KafkaConsumerOptions
                {
                    BootstrapServers =
                        sourceTopic.BootstrapServers,

                    StockDepletedTopic =
                        sourceTopic.Name,

                    StockDepletedDeadLetterTopic =
                        $"{sourceTopic.Name}.dlq",

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
            new FailingDeadLetterPublisher();

        using var worker =
            new StockDepletedConsumerWorker(
                kafkaOptions,
                serviceScopeFactory,
                retryExecutor,
                deadLetterPublisher,
                NullLogger<
                    StockDepletedConsumerWorker>.Instance);

        var integrationEvent =
            new StockDepletedIntegrationEvent(
                Guid.NewGuid(),
                DateTime.UtcNow,
                Guid.NewGuid());

        var originalPayload =
            JsonSerializer.Serialize(
                integrationEvent,
                SerializerOptions);

        await worker.StartAsync(
            CancellationToken.None);

        using (var producer =
            new ProducerBuilder<string, string>(
                new ProducerConfig
                {
                    BootstrapServers =
                        sourceTopic.BootstrapServers,

                    Acks =
                        Acks.All
                })
                .Build())
        {
            await producer.ProduceAsync(
                sourceTopic.Name,
                new Message<string, string>
                {
                    Key =
                        integrationEvent.EventId.ToString(
                            "D"),

                    Value =
                        originalPayload
                });
        }

        await deadLetterPublisher.WaitAsync(
            TimeSpan.FromSeconds(15));

        Assert.Equal(
            3,
            processor.InvocationCount);

        Assert.Equal(
            1,
            deadLetterPublisher.InvocationCount);

        try
        {
            await worker.StopAsync(
                CancellationToken.None);
        }
        catch (InvalidOperationException exception)
        {
            Assert.Equal(
                "Simulated dead-letter publish failure.",
                exception.Message);
        }

        using var verificationConsumer =
            new ConsumerBuilder<string, string>(
                new ConsumerConfig
                {
                    BootstrapServers =
                        sourceTopic.BootstrapServers,

                    GroupId =
                        consumerGroupId,

                    AutoOffsetReset =
                        AutoOffsetReset.Earliest,

                    EnableAutoCommit =
                        false
                })
                .Build();

        verificationConsumer.Subscribe(
            sourceTopic.Name);

        var replayedMessage =
            verificationConsumer.Consume(
                TimeSpan.FromSeconds(10));

        Assert.NotNull(
            replayedMessage);

        var replayedEvent =
            JsonSerializer.Deserialize<
                StockDepletedIntegrationEvent>(
                    replayedMessage.Message.Value,
                    SerializerOptions);

        Assert.NotNull(
            replayedEvent);

        Assert.Equal(
            integrationEvent.EventId,
            replayedEvent.EventId);

        Assert.Equal(
            integrationEvent.ProductId,
            replayedEvent.ProductId);

        verificationConsumer.Close();
    }

    [Fact]
    public async Task ConsumerWorker_ShouldPublishPoisonMessageToDeadLetterAndCommitOffset_WhenPayloadIsMalformed()
    {
        await using var sourceTopic =
            await KafkaTestTopic.CreateAsync();

        await using var deadLetterTopic =
            await KafkaTestTopic.CreateAsync();

        var consumerGroupId =
            $"flashsale-poison-tests-{Guid.NewGuid():N}";

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

        var kafkaOptions =
            Options.Create(
                new KafkaConsumerOptions
                {
                    BootstrapServers =
                        sourceTopic.BootstrapServers,

                    StockDepletedTopic =
                        sourceTopic.Name,

                    StockDepletedDeadLetterTopic =
                        deadLetterTopic.Name,

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

        using var deadLetterPublisher =
            new KafkaDeadLetterPublisher(
                kafkaOptions,
                NullLogger<
                    KafkaDeadLetterPublisher>.Instance);

        using var worker =
            new StockDepletedConsumerWorker(
                kafkaOptions,
                serviceScopeFactory,
                retryExecutor,
                deadLetterPublisher,
                NullLogger<
                    StockDepletedConsumerWorker>.Instance);

        using var deadLetterConsumer =
            new ConsumerBuilder<string, string>(
                new ConsumerConfig
                {
                    BootstrapServers =
                        deadLetterTopic.BootstrapServers,

                    GroupId =
                        $"flashsale-poison-reader-{Guid.NewGuid():N}",

                    AutoOffsetReset =
                        AutoOffsetReset.Earliest,

                    EnableAutoCommit =
                        false
                })
                .Build();

        deadLetterConsumer.Subscribe(
            deadLetterTopic.Name);

        await worker.StartAsync(
            CancellationToken.None);

        const string originalKey =
            "invalid-stock-event";

        const string malformedPayload =
            "{ this-is-not-valid-json";

        try
        {
            using var producer =
                new ProducerBuilder<string, string>(
                    new ProducerConfig
                    {
                        BootstrapServers =
                            sourceTopic.BootstrapServers,

                        Acks =
                            Acks.All
                    })
                    .Build();

            await producer.ProduceAsync(
                sourceTopic.Name,
                new Message<string, string>
                {
                    Key =
                        originalKey,

                    Value =
                        malformedPayload
                });

            var deadLetterResult =
                deadLetterConsumer.Consume(
                    TimeSpan.FromSeconds(15));

            Assert.NotNull(
                deadLetterResult);

            Assert.Equal(
                originalKey,
                deadLetterResult.Message.Key);

            var deadLetterMessage =
                JsonSerializer.Deserialize<
                    DeadLetterMessage>(
                        deadLetterResult.Message.Value,
                        SerializerOptions);

            Assert.NotNull(
                deadLetterMessage);

            Assert.Null(
                deadLetterMessage.EventId);

            Assert.Null(
                deadLetterMessage.EventType);

            Assert.Equal(
                originalKey,
                deadLetterMessage.OriginalKey);

            Assert.Equal(
                sourceTopic.Name,
                deadLetterMessage.OriginalTopic);

            Assert.Equal(
                0,
                deadLetterMessage.OriginalPartition);

            Assert.Equal(
                0,
                deadLetterMessage.OriginalOffset);

            Assert.Equal(
                malformedPayload,
                deadLetterMessage.Payload);

            Assert.Contains(
                nameof(JsonException),
                deadLetterMessage.ErrorType);

            Assert.False(
                string.IsNullOrWhiteSpace(
                    deadLetterMessage.ErrorMessage));

            Assert.Equal(
                DateTimeKind.Utc,
                deadLetterMessage.FailedAtUtc.Kind);

            Assert.True(
                deadLetterMessage.FailedAtUtc <=
                DateTime.UtcNow);

            Assert.Equal(
                0,
                processor.InvocationCount);

            await Task.Delay(
                TimeSpan.FromMilliseconds(250));
        }
        finally
        {
            await worker.StopAsync(
                CancellationToken.None);

            deadLetterConsumer.Close();
        }

        using var verificationConsumer =
            new ConsumerBuilder<string, string>(
                new ConsumerConfig
                {
                    BootstrapServers =
                        sourceTopic.BootstrapServers,

                    GroupId =
                        consumerGroupId,

                    AutoOffsetReset =
                        AutoOffsetReset.Earliest,

                    EnableAutoCommit =
                        false
                })
                .Build();

        verificationConsumer.Subscribe(
            sourceTopic.Name);

        var replayedMessage =
            verificationConsumer.Consume(
                TimeSpan.FromSeconds(5));

        Assert.Null(
            replayedMessage);

        verificationConsumer.Close();
    }

    private sealed class AlwaysFailingIntegrationEventProcessor
        : IIntegrationEventProcessor<
            StockDepletedIntegrationEvent>
    {
        public int InvocationCount { get; private set; }

        public Task<IntegrationEventProcessingResult> ProcessAsync(
            StockDepletedIntegrationEvent integrationEvent,
            CancellationToken cancellationToken = default)
        {
            InvocationCount++;

            throw new InvalidOperationException(
                "Simulated permanent processing failure.");
        }
    }

    private sealed class RecordingIntegrationEventProcessor
        : IIntegrationEventProcessor<
            StockDepletedIntegrationEvent>
    {
        public int InvocationCount { get; private set; }

        public Task<IntegrationEventProcessingResult> ProcessAsync(
            StockDepletedIntegrationEvent integrationEvent,
            CancellationToken cancellationToken = default)
        {
            InvocationCount++;

            return Task.FromResult(
                IntegrationEventProcessingResult.Processed);
        }
    }

    private sealed class FailingDeadLetterPublisher
        : IDeadLetterPublisher
    {
        private readonly TaskCompletionSource<bool>
            _publishAttempted =
                new(
                    TaskCreationOptions
                        .RunContinuationsAsynchronously);

        public int InvocationCount { get; private set; }

        public Task PublishAsync(
            DeadLetterMessage message,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(
                message);

            InvocationCount++;

            _publishAttempted.TrySetResult(
                true);

            return Task.FromException(
                new InvalidOperationException(
                    "Simulated dead-letter publish failure."));
        }

        public Task WaitAsync(
            TimeSpan timeout)
        {
            return _publishAttempted
                .Task
                .WaitAsync(
                    timeout);
        }
    }
}