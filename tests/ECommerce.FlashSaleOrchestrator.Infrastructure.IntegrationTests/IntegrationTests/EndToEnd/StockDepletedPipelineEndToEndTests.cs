using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using ECommerce.FlashSaleOrchestrator.Api.BackgroundServices;
using ECommerce.FlashSaleOrchestrator.Application.Abstractions.AlternativeCandidates;
using ECommerce.FlashSaleOrchestrator.Infrastructure.AI;
using ECommerce.FlashSaleOrchestrator.Infrastructure.AI.SemanticCaching;
using ECommerce.FlashSaleOrchestrator.Infrastructure.IntegrationTests.AI;
using Microsoft.SemanticKernel.ChatCompletion;
using ECommerce.FlashSaleOrchestrator.Application.Abstractions.Messaging;
using ECommerce.FlashSaleOrchestrator.Application.Abstractions.Observability;
using ECommerce.FlashSaleOrchestrator.Application.AlternativeCandidates;
using ECommerce.FlashSaleOrchestrator.Application.AlternativeRecommendations;
using ECommerce.FlashSaleOrchestrator.Application.AlternativeRecommendations.SemanticCaching;
using ECommerce.FlashSaleOrchestrator.Application.IntegrationEvents.Inventory;
using ECommerce.FlashSaleOrchestrator.Domain.Inventory;
using ECommerce.FlashSaleOrchestrator.Domain.Inventory.Events;
using ECommerce.FlashSaleOrchestrator.Domain.Products;
using ECommerce.FlashSaleOrchestrator.Infrastructure.AlternativeCandidates;
using ECommerce.FlashSaleOrchestrator.Infrastructure.IntegrationTests.Kafka;
using ECommerce.FlashSaleOrchestrator.Infrastructure.IntegrationTests.Outbox;
using ECommerce.FlashSaleOrchestrator.Infrastructure.Messaging.Kafka;
using ECommerce.FlashSaleOrchestrator.Infrastructure.Observability;
using ECommerce.FlashSaleOrchestrator.Infrastructure.Persistence;
using ECommerce.FlashSaleOrchestrator.Infrastructure.Persistence.Inbox;
using ECommerce.FlashSaleOrchestrator.Infrastructure.Persistence.Outbox;
using ECommerce.FlashSaleOrchestrator.Worker.BackgroundServices;
using ECommerce.FlashSaleOrchestrator.Worker.IntegrationEvents.Inventory;
using ECommerce.FlashSaleOrchestrator.Worker.Messaging.DeadLetter;
using ECommerce.FlashSaleOrchestrator.Worker.Messaging.Kafka;
using ECommerce.FlashSaleOrchestrator.Worker.Resilience;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ECommerce.FlashSaleOrchestrator.Infrastructure.IntegrationTests.EndToEnd;

public sealed class StockDepletedPipelineEndToEndTests
{
    [Fact]
    public async Task Pipeline_ShouldPublishConsumeAndProcessStockDepletedEvent()
    {
        await using var database =
            await OutboxTestDatabase.CreateAsync();

        await using var topic =
            await KafkaTestTopic.CreateAsync();

        var eventId =
            Guid.NewGuid();

        var productId =
            Guid.NewGuid();

        const string correlationId =
            "e2e-correlation-123";

        var occurredAtUtc =
            DateTime.UtcNow;

        await SeedPendingOutboxMessageAsync(
            database,
            eventId,
            productId,
            occurredAtUtc,
            correlationId);

        var handlerProbe =
            new HandlerProbe();

        await using var serviceProvider =
            CreateServiceProvider(
                database,
                topic,
                handlerProbe);

        var serviceScopeFactory =
            serviceProvider.GetRequiredService<
                IServiceScopeFactory>();

        using var outboxWorker =
            CreateOutboxWorker(
                serviceScopeFactory);

        var deadLetterPublisher =
            new RecordingDeadLetterPublisher();

        using var consumerWorker =
            CreateConsumerWorker(
                topic,
                serviceScopeFactory,
                deadLetterPublisher,
                $"flashsale-e2e-{Guid.NewGuid():N}");

        await consumerWorker.StartAsync(
            CancellationToken.None);

        await outboxWorker.StartAsync(
            CancellationToken.None);

        ProcessedDelivery processedDelivery;

        try
        {
            await WaitUntilAsync(
                async () =>
                {
                    await using var context =
                        database.CreateContext();

                    return await context
                        .OutboxMessages
                        .AsNoTracking()
                        .AnyAsync(
                            message =>
                                message.Id == eventId
                                && message.ProcessedAtUtc != null);
                },
                TimeSpan.FromSeconds(10));

            try
            {
                processedDelivery =
                    await handlerProbe.WaitAsync(
                        TimeSpan.FromSeconds(15));
            }
            catch (TimeoutException)
                when (deadLetterPublisher.LastMessage is not null)
            {
                throw CreateDeadLetterException(
                    deadLetterPublisher.LastMessage);
            }

            await WaitUntilAsync(
                async () =>
                {
                    await using var context =
                        database.CreateContext();

                    return await context
                        .InboxMessages
                        .AsNoTracking()
                        .AnyAsync(
                            message =>
                                message.Id == eventId
                                && message.ProcessedAtUtc != null);
                },
                TimeSpan.FromSeconds(10));
        }
        finally
        {
            await StopWorkersAsync(
                outboxWorker,
                consumerWorker);
        }

        Assert.Equal(
            eventId,
            processedDelivery.IntegrationEvent.EventId);

        Assert.Equal(
            productId,
            processedDelivery.IntegrationEvent.ProductId);

        Assert.Equal(
            StockDepletedIntegrationEvent.EventTypeName,
            processedDelivery.IntegrationEvent.EventType);

        Assert.Equal(
            correlationId,
            processedDelivery.IntegrationEvent.CorrelationId);

        Assert.Equal(
            correlationId,
            processedDelivery.ScopedCorrelationId);

        Assert.Equal(
            DateTimeKind.Utc,
            processedDelivery.IntegrationEvent.OccurredAtUtc.Kind);

        Assert.Equal(
            0,
            deadLetterPublisher.InvocationCount);

        await using var verificationContext =
            database.CreateContext();

        var persistedOutboxMessage =
            await verificationContext
                .OutboxMessages
                .AsNoTracking()
                .SingleAsync(
                    message =>
                        message.Id == eventId);

        Assert.NotNull(
            persistedOutboxMessage.ProcessedAtUtc);

        Assert.Equal(
            correlationId,
            persistedOutboxMessage.CorrelationId);

        var persistedInboxMessage =
            await verificationContext
                .InboxMessages
                .AsNoTracking()
                .SingleAsync(
                    message =>
                        message.Id == eventId);

        Assert.Equal(
            StockDepletedIntegrationEvent.EventTypeName,
            persistedInboxMessage.Type);

        Assert.NotNull(
            persistedInboxMessage.ProcessedAtUtc);

        Assert.Equal(
            1,
            handlerProbe.InvocationCount);
    }

    [Fact]
    public async Task Pipeline_ShouldProcessHandlerOnlyOnce_WhenEventIsDeliveredTwice()
    {
        await using var database =
            await OutboxTestDatabase.CreateAsync();

        await using var topic =
            await KafkaTestTopic.CreateAsync();

        var eventId =
            Guid.NewGuid();

        var productId =
            Guid.NewGuid();

        const string correlationId =
            "e2e-duplicate-correlation-123";

        var occurredAtUtc =
            DateTime.UtcNow;

        await SeedPendingOutboxMessageAsync(
            database,
            eventId,
            productId,
            occurredAtUtc,
            correlationId);

        var handlerProbe =
            new HandlerProbe();

        var processingResultProbe =
            new ProcessingResultProbe();

        await using var serviceProvider =
            CreateServiceProvider(
                database,
                topic,
                handlerProbe,
                processingResultProbe);

        var serviceScopeFactory =
            serviceProvider.GetRequiredService<
                IServiceScopeFactory>();

        using var outboxWorker =
            CreateOutboxWorker(
                serviceScopeFactory);

        var deadLetterPublisher =
            new RecordingDeadLetterPublisher();

        using var consumerWorker =
            CreateConsumerWorker(
                topic,
                serviceScopeFactory,
                deadLetterPublisher,
                $"flashsale-e2e-duplicate-{Guid.NewGuid():N}");

        await consumerWorker.StartAsync(
            CancellationToken.None);

        await outboxWorker.StartAsync(
            CancellationToken.None);

        ProcessingObservation firstProcessing;
        ProcessingObservation secondProcessing;

        try
        {
            try
            {
                firstProcessing =
                    await processingResultProbe.WaitForFirstAsync(
                        TimeSpan.FromSeconds(15));
            }
            catch (TimeoutException)
                when (deadLetterPublisher.LastMessage is not null)
            {
                throw CreateDeadLetterException(
                    deadLetterPublisher.LastMessage);
            }

            Assert.Equal(
                eventId,
                firstProcessing.EventId);

            Assert.Equal(
                IntegrationEventProcessingResult.Processed,
                firstProcessing.Result);

            await WaitUntilAsync(
                async () =>
                {
                    await using var context =
                        database.CreateContext();

                    return await context
                        .OutboxMessages
                        .AsNoTracking()
                        .AnyAsync(
                            message =>
                                message.Id == eventId
                                && message.ProcessedAtUtc != null);
                },
                TimeSpan.FromSeconds(10));

            var duplicateIntegrationEvent =
                new StockDepletedIntegrationEvent(
                    eventId,
                    occurredAtUtc,
                    productId,
                    correlationId);

            var eventPublisher =
                serviceProvider.GetRequiredService<
                    IEventPublisher>();

            await eventPublisher.PublishAsync(
                duplicateIntegrationEvent);

            try
            {
                secondProcessing =
                    await processingResultProbe.WaitForSecondAsync(
                        TimeSpan.FromSeconds(15));
            }
            catch (TimeoutException)
                when (deadLetterPublisher.LastMessage is not null)
            {
                throw CreateDeadLetterException(
                    deadLetterPublisher.LastMessage);
            }
        }
        finally
        {
            await StopWorkersAsync(
                outboxWorker,
                consumerWorker);
        }

        Assert.Equal(
            eventId,
            secondProcessing.EventId);

        Assert.Equal(
            IntegrationEventProcessingResult.AlreadyProcessed,
            secondProcessing.Result);

        Assert.Equal(
            1,
            handlerProbe.InvocationCount);

        Assert.Equal(
            0,
            deadLetterPublisher.InvocationCount);

        await using var verificationContext =
            database.CreateContext();

        var persistedOutboxMessage =
            await verificationContext
                .OutboxMessages
                .AsNoTracking()
                .SingleAsync(
                    message =>
                        message.Id == eventId);

        Assert.NotNull(
            persistedOutboxMessage.ProcessedAtUtc);

        Assert.Equal(
            correlationId,
            persistedOutboxMessage.CorrelationId);

        var inboxMessages =
            await verificationContext
                .InboxMessages
                .AsNoTracking()
                .Where(
                    message =>
                        message.Id == eventId)
                .ToListAsync();

        Assert.Single(
            inboxMessages);

        Assert.Equal(
            StockDepletedIntegrationEvent.EventTypeName,
            inboxMessages[0].Type);

        Assert.NotNull(
            inboxMessages[0].ProcessedAtUtc);
    }

    [Fact]
    public async Task Pipeline_ShouldRetrieveEligibleCandidates_WhenStockDepletedEventIsProcessed()
    {
        await using var database =
            await OutboxTestDatabase.CreateAsync();

        await using var topic =
            await KafkaTestTopic.CreateAsync();

        var eventId =
            Guid.NewGuid();

        var depletedProductId =
            Guid.NewGuid();

        var highStockCandidateId =
            Guid.NewGuid();

        var lowStockCandidateId =
            Guid.NewGuid();

        var differentCategoryProductId =
            Guid.NewGuid();

        const string correlationId =
            "e2e-candidate-correlation-123";

        var occurredAtUtc =
            DateTime.UtcNow;

        await SeedCandidateProductsAsync(
            database,
            depletedProductId,
            highStockCandidateId,
            lowStockCandidateId,
            differentCategoryProductId);

        await SeedPendingOutboxMessageAsync(
            database,
            eventId,
            depletedProductId,
            occurredAtUtc,
            correlationId);

        var candidateProbe =
            new CandidateProbe();

        await using var serviceProvider =
            CreateCandidateRetrievalServiceProvider(
                database,
                topic,
                candidateProbe);

        var serviceScopeFactory =
            serviceProvider.GetRequiredService<
                IServiceScopeFactory>();

        using var outboxWorker =
            CreateOutboxWorker(
                serviceScopeFactory);

        var deadLetterPublisher =
            new RecordingDeadLetterPublisher();

        using var consumerWorker =
            CreateConsumerWorker(
                topic,
                serviceScopeFactory,
                deadLetterPublisher,
                $"flashsale-e2e-candidates-{Guid.NewGuid():N}");

        await consumerWorker.StartAsync(
            CancellationToken.None);

        await outboxWorker.StartAsync(
            CancellationToken.None);

        IReadOnlyList<AlternativeCandidate> candidates;

        try
        {
            try
            {
                candidates =
                    await candidateProbe.WaitAsync(
                        TimeSpan.FromSeconds(15));
            }
            catch (TimeoutException)
                when (deadLetterPublisher.LastMessage is not null)
            {
                throw CreateDeadLetterException(
                    deadLetterPublisher.LastMessage);
            }

            await WaitUntilAsync(
                async () =>
                {
                    await using var context =
                        database.CreateContext();

                    return await context
                        .InboxMessages
                        .AsNoTracking()
                        .AnyAsync(
                            message =>
                                message.Id == eventId
                                && message.ProcessedAtUtc != null);
                },
                TimeSpan.FromSeconds(10));
        }
        finally
        {
            await StopWorkersAsync(
                outboxWorker,
                consumerWorker);
        }

        Assert.Equal(
            2,
            candidates.Count);

        Assert.Collection(
            candidates,
            candidate =>
            {
                Assert.Equal(
                    highStockCandidateId,
                    candidate.ProductId);

                Assert.Equal(
                    "High Stock Mouse",
                    candidate.Name);

                Assert.Equal(
                    "mouse",
                    candidate.Category);

                Assert.Equal(
                    20,
                    candidate.AvailableQuantity);
            },
            candidate =>
            {
                Assert.Equal(
                    lowStockCandidateId,
                    candidate.ProductId);

                Assert.Equal(
                    "Low Stock Mouse",
                    candidate.Name);

                Assert.Equal(
                    "mouse",
                    candidate.Category);

                Assert.Equal(
                    5,
                    candidate.AvailableQuantity);
            });

        Assert.DoesNotContain(
            candidates,
            candidate =>
                candidate.ProductId ==
                depletedProductId);

        Assert.DoesNotContain(
            candidates,
            candidate =>
                candidate.ProductId ==
                differentCategoryProductId);

        Assert.Equal(
            1,
            candidateProbe.InvocationCount);

        Assert.Equal(
            0,
            deadLetterPublisher.InvocationCount);

        await using var verificationContext =
            database.CreateContext();

        var outboxMessage =
            await verificationContext
                .OutboxMessages
                .AsNoTracking()
                .SingleAsync(
                    message =>
                        message.Id == eventId);

        Assert.NotNull(
            outboxMessage.ProcessedAtUtc);

        var inboxMessage =
            await verificationContext
                .InboxMessages
                .AsNoTracking()
                .SingleAsync(
                    message =>
                        message.Id == eventId);

        Assert.NotNull(
            inboxMessage.ProcessedAtUtc);
    }

    [Fact]
    public async Task Pipeline_ShouldUseDeterministicFallbackWithoutDeadLetter_WhenLlmResponseRemainsInvalid()
    {
        await using var database =
            await OutboxTestDatabase.CreateAsync();

        await using var topic =
            await KafkaTestTopic.CreateAsync();

        var eventId =
            Guid.NewGuid();

        var depletedProductId =
            Guid.NewGuid();

        var highStockCandidateId =
            Guid.NewGuid();

        var lowStockCandidateId =
            Guid.NewGuid();

        var differentCategoryProductId =
            Guid.NewGuid();

        const string correlationId =
            "e2e-recommendation-fallback-correlation";

        var occurredAtUtc =
            DateTime.UtcNow;

        await SeedCandidateProductsAsync(
            database,
            depletedProductId,
            highStockCandidateId,
            lowStockCandidateId,
            differentCategoryProductId);

        await SeedPendingOutboxMessageAsync(
            database,
            eventId,
            depletedProductId,
            occurredAtUtc,
            correlationId);

        var fakeChatCompletionService =
            new FakeChatCompletionService(
                "{ invalid-json");

        await using var serviceProvider =
            CreateRecommendationFallbackServiceProvider(
                database,
                topic,
                fakeChatCompletionService);

        var serviceScopeFactory =
            serviceProvider.GetRequiredService<
                IServiceScopeFactory>();

        using var outboxWorker =
            CreateOutboxWorker(
                serviceScopeFactory);

        var deadLetterPublisher =
            new RecordingDeadLetterPublisher();

        using var consumerWorker =
            CreateConsumerWorker(
                topic,
                serviceScopeFactory,
                deadLetterPublisher,
                $"flashsale-e2e-fallback-{Guid.NewGuid():N}");

        await consumerWorker.StartAsync(
            CancellationToken.None);

        await outboxWorker.StartAsync(
            CancellationToken.None);

        try
        {
            await WaitUntilAsync(
                async () =>
                {
                    await using var context =
                        database.CreateContext();

                    return await context
                        .InboxMessages
                        .AsNoTracking()
                        .AnyAsync(
                            message =>
                                message.Id == eventId
                                && message.ProcessedAtUtc != null);
                },
                TimeSpan.FromSeconds(15));
        }
        finally
        {
            await StopWorkersAsync(
                outboxWorker,
                consumerWorker);
        }

        Assert.Equal(
            2,
            fakeChatCompletionService.CallCount);

        Assert.Equal(
            0,
            deadLetterPublisher.InvocationCount);

        await using var verificationContext =
            database.CreateContext();

        var inboxMessage =
            await verificationContext
                .InboxMessages
                .AsNoTracking()
                .SingleAsync(
                    message =>
                        message.Id == eventId);

        Assert.Equal(
            StockDepletedIntegrationEvent.EventTypeName,
            inboxMessage.Type);

        Assert.NotNull(
            inboxMessage.ProcessedAtUtc);

        var outboxMessage =
            await verificationContext
                .OutboxMessages
                .AsNoTracking()
                .SingleAsync(
                    message =>
                        message.Id == eventId);

        Assert.NotNull(
            outboxMessage.ProcessedAtUtc);
    }

    [Fact]
    public async Task Pipeline_ShouldRetryAndPublishToDeadLetter_WhenHandlerKeepsFailing()
    {
        await using var database =
            await OutboxTestDatabase.CreateAsync();

        await using var topic =
            await KafkaTestTopic.CreateAsync();

        var eventId =
            Guid.NewGuid();

        var productId =
            Guid.NewGuid();

        const string correlationId =
            "e2e-failure-correlation-123";

        var occurredAtUtc =
            DateTime.UtcNow;

        await SeedPendingOutboxMessageAsync(
            database,
            eventId,
            productId,
            occurredAtUtc,
            correlationId);

        var failureProbe =
            new FailureProbe();

        await using var serviceProvider =
            CreateFailureServiceProvider(
                database,
                topic,
                failureProbe);

        var serviceScopeFactory =
            serviceProvider.GetRequiredService<
                IServiceScopeFactory>();

        using var outboxWorker =
            CreateOutboxWorker(
                serviceScopeFactory);

        var deadLetterPublisher =
            new RecordingDeadLetterPublisher();

        using var consumerWorker =
            CreateConsumerWorker(
                topic,
                serviceScopeFactory,
                deadLetterPublisher,
                $"flashsale-e2e-failure-{Guid.NewGuid():N}");

        await consumerWorker.StartAsync(
            CancellationToken.None);

        await outboxWorker.StartAsync(
            CancellationToken.None);

        DeadLetterMessage deadLetterMessage;

        try
        {
            await WaitUntilAsync(
                async () =>
                {
                    await using var context =
                        database.CreateContext();

                    return await context
                        .OutboxMessages
                        .AsNoTracking()
                        .AnyAsync(
                            message =>
                                message.Id == eventId
                                && message.ProcessedAtUtc != null);
                },
                TimeSpan.FromSeconds(10));

            deadLetterMessage =
                await deadLetterPublisher.WaitAsync(
                    TimeSpan.FromSeconds(15));
        }
        finally
        {
            await StopWorkersAsync(
                outboxWorker,
                consumerWorker);
        }

        Assert.Equal(
            3,
            failureProbe.InvocationCount);

        Assert.Equal(
            1,
            deadLetterPublisher.InvocationCount);

        Assert.Equal(
            eventId,
            deadLetterMessage.EventId);

        Assert.Equal(
            StockDepletedIntegrationEvent.EventTypeName,
            deadLetterMessage.EventType);

        Assert.Equal(
            correlationId,
            deadLetterMessage.CorrelationId);

        Assert.Equal(
            eventId.ToString("D"),
            deadLetterMessage.OriginalKey);

        Assert.Equal(
            topic.Name,
            deadLetterMessage.OriginalTopic);

        Assert.Equal(
            typeof(InvalidOperationException).FullName,
            deadLetterMessage.ErrorType);

        Assert.Contains(
            "Simulated E2E handler failure.",
            deadLetterMessage.ErrorMessage);

        Assert.All(
            failureProbe.CorrelationIds,
            observedCorrelationId =>
                Assert.Equal(
                    correlationId,
                    observedCorrelationId));

        await using var verificationContext =
            database.CreateContext();

        var persistedOutboxMessage =
            await verificationContext
                .OutboxMessages
                .AsNoTracking()
                .SingleAsync(
                    message =>
                        message.Id == eventId);

        Assert.NotNull(
            persistedOutboxMessage.ProcessedAtUtc);

        Assert.Equal(
            correlationId,
            persistedOutboxMessage.CorrelationId);

        var inboxMessages =
            await verificationContext
                .InboxMessages
                .AsNoTracking()
                .Where(
                    message =>
                        message.Id == eventId)
                .ToListAsync();

        Assert.Empty(
            inboxMessages);
    }

    private static ServiceProvider CreateServiceProvider(
        OutboxTestDatabase database,
        KafkaTestTopic topic,
        HandlerProbe handlerProbe,
        ProcessingResultProbe? processingResultProbe = null)
    {
        var services =
            new ServiceCollection();

        services.AddLogging();

        services.AddSingleton(
            handlerProbe);

        services.AddScoped<
            ICorrelationContext,
            CorrelationContext>();

        services.AddDbContext<
            FlashSaleOrchestratorDbContext>(
            options =>
                options.UseSqlServer(
                    database.ConnectionString));

        services.AddSingleton<
            StockDepletedOutboxMessageMapper>();

        services.AddScoped<
            OutboxProcessor>();

        services.AddSingleton<
            IOptions<KafkaPublisherOptions>>(
            Options.Create(
                new KafkaPublisherOptions
                {
                    BootstrapServers =
                        topic.BootstrapServers,

                    StockDepletedTopic =
                        topic.Name
                }));

        services.AddSingleton<
            IEventPublisher,
            KafkaEventPublisher>();

        services.AddScoped<
            IIntegrationEventHandler<
                StockDepletedIntegrationEvent>,
            RecordingStockDepletedHandler>();

        if (processingResultProbe is null)
        {
            services.AddScoped<
                IIntegrationEventProcessor<
                    StockDepletedIntegrationEvent>,
                InboxIntegrationEventProcessor<
                    StockDepletedIntegrationEvent>>();
        }
        else
        {
            services.AddSingleton(
                processingResultProbe);

            services.AddScoped<
                InboxIntegrationEventProcessor<
                    StockDepletedIntegrationEvent>>();

            services.AddScoped<
                IIntegrationEventProcessor<
                    StockDepletedIntegrationEvent>,
                RecordingIntegrationEventProcessor>();
        }

        return services.BuildServiceProvider();
    }

    private static ServiceProvider CreateFailureServiceProvider(
        OutboxTestDatabase database,
        KafkaTestTopic topic,
        FailureProbe failureProbe)
    {
        var services =
            new ServiceCollection();

        services.AddLogging();

        services.AddSingleton(
            failureProbe);

        services.AddScoped<
            ICorrelationContext,
            CorrelationContext>();

        services.AddDbContext<
            FlashSaleOrchestratorDbContext>(
            options =>
                options.UseSqlServer(
                    database.ConnectionString));

        services.AddSingleton<
            StockDepletedOutboxMessageMapper>();

        services.AddScoped<
            OutboxProcessor>();

        services.AddSingleton<
            IOptions<KafkaPublisherOptions>>(
            Options.Create(
                new KafkaPublisherOptions
                {
                    BootstrapServers =
                        topic.BootstrapServers,

                    StockDepletedTopic =
                        topic.Name
                }));

        services.AddSingleton<
            IEventPublisher,
            KafkaEventPublisher>();

        services.AddScoped<
            IIntegrationEventHandler<
                StockDepletedIntegrationEvent>,
            FailingStockDepletedHandler>();

        services.AddScoped<
            IIntegrationEventProcessor<
                StockDepletedIntegrationEvent>,
            InboxIntegrationEventProcessor<
                StockDepletedIntegrationEvent>>();

        return services.BuildServiceProvider();
    }

    private static OutboxPublisherWorker CreateOutboxWorker(
        IServiceScopeFactory serviceScopeFactory)
    {
        return new OutboxPublisherWorker(
            serviceScopeFactory,
            Options.Create(
                new OutboxPublisherOptions
                {
                    BatchSize =
                        10,

                    PollingInterval =
                        TimeSpan.FromMilliseconds(
                            100)
                }),
            NullLogger<
                OutboxPublisherWorker>.Instance);
    }

    private static StockDepletedConsumerWorker CreateConsumerWorker(
        KafkaTestTopic topic,
        IServiceScopeFactory serviceScopeFactory,
        IDeadLetterPublisher deadLetterPublisher,
        string consumerGroupId)
    {
        var consumerOptions =
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

        return new StockDepletedConsumerWorker(
            consumerOptions,
            serviceScopeFactory,
            retryExecutor,
            deadLetterPublisher,
            NullLogger<
                StockDepletedConsumerWorker>.Instance);
    }

    private static async Task SeedPendingOutboxMessageAsync(
        OutboxTestDatabase database,
        Guid eventId,
        Guid productId,
        DateTime occurredAtUtc,
        string correlationId)
    {
        Assert.Equal(
            DateTimeKind.Utc,
            occurredAtUtc.Kind);

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
                eventId,
                occurredAtUtc,
                eventType,
                payload,
                correlationId);

        await using var context =
            database.CreateContext();

        context.OutboxMessages.Add(
            outboxMessage);

        await context.SaveChangesAsync();
    }

    private static async Task WaitUntilAsync(
        Func<Task<bool>> condition,
        TimeSpan timeout)
    {
        var stopwatch =
            Stopwatch.StartNew();

        while (stopwatch.Elapsed < timeout)
        {
            if (await condition())
            {
                return;
            }

            await Task.Delay(
                TimeSpan.FromMilliseconds(
                    100));
        }

        throw new TimeoutException(
            $"Condition was not satisfied within {timeout}.");
    }

    private static InvalidOperationException CreateDeadLetterException(
        DeadLetterMessage deadLetterMessage)
    {
        return new InvalidOperationException(
            "Consumer moved the E2E message to dead-letter processing. " +
            $"ErrorType: {deadLetterMessage.ErrorType}. " +
            $"ErrorMessage: {deadLetterMessage.ErrorMessage}");
    }

    private static async Task StopWorkersAsync(
        OutboxPublisherWorker outboxWorker,
        StockDepletedConsumerWorker consumerWorker)
    {
        using var outboxStopCancellation =
            new CancellationTokenSource(
                TimeSpan.FromSeconds(5));

        await outboxWorker.StopAsync(
            outboxStopCancellation.Token);

        using var consumerStopCancellation =
            new CancellationTokenSource(
                TimeSpan.FromSeconds(5));

        await consumerWorker.StopAsync(
            consumerStopCancellation.Token);
    }

    private sealed class RecordingStockDepletedHandler
        : IIntegrationEventHandler<
            StockDepletedIntegrationEvent>
    {
        private readonly ICorrelationContext
            _correlationContext;

        private readonly HandlerProbe
            _handlerProbe;

        public RecordingStockDepletedHandler(
            ICorrelationContext correlationContext,
            HandlerProbe handlerProbe)
        {
            ArgumentNullException.ThrowIfNull(
                correlationContext);

            ArgumentNullException.ThrowIfNull(
                handlerProbe);

            _correlationContext =
                correlationContext;

            _handlerProbe =
                handlerProbe;
        }

        public Task HandleAsync(
            StockDepletedIntegrationEvent integrationEvent,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(
                integrationEvent);

            _handlerProbe.Record(
                integrationEvent,
                _correlationContext.CorrelationId);

            return Task.CompletedTask;
        }
    }

    private sealed class FailingStockDepletedHandler
        : IIntegrationEventHandler<
            StockDepletedIntegrationEvent>
    {
        private readonly ICorrelationContext
            _correlationContext;

        private readonly FailureProbe
            _failureProbe;

        public FailingStockDepletedHandler(
            ICorrelationContext correlationContext,
            FailureProbe failureProbe)
        {
            ArgumentNullException.ThrowIfNull(
                correlationContext);

            ArgumentNullException.ThrowIfNull(
                failureProbe);

            _correlationContext =
                correlationContext;

            _failureProbe =
                failureProbe;
        }

        public Task HandleAsync(
            StockDepletedIntegrationEvent integrationEvent,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(
                integrationEvent);

            _failureProbe.Record(
                _correlationContext.CorrelationId);

            throw new InvalidOperationException(
                "Simulated E2E handler failure.");
        }
    }

    private sealed class RecordingIntegrationEventProcessor
        : IIntegrationEventProcessor<
            StockDepletedIntegrationEvent>
    {
        private readonly InboxIntegrationEventProcessor<
            StockDepletedIntegrationEvent> _innerProcessor;

        private readonly ProcessingResultProbe
            _processingResultProbe;

        public RecordingIntegrationEventProcessor(
            InboxIntegrationEventProcessor<
                StockDepletedIntegrationEvent> innerProcessor,
            ProcessingResultProbe processingResultProbe)
        {
            ArgumentNullException.ThrowIfNull(
                innerProcessor);

            ArgumentNullException.ThrowIfNull(
                processingResultProbe);

            _innerProcessor =
                innerProcessor;

            _processingResultProbe =
                processingResultProbe;
        }

        public async Task<IntegrationEventProcessingResult> ProcessAsync(
            StockDepletedIntegrationEvent integrationEvent,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(
                integrationEvent);

            var result =
                await _innerProcessor.ProcessAsync(
                    integrationEvent,
                    cancellationToken);

            _processingResultProbe.Record(
                new ProcessingObservation(
                    integrationEvent.EventId,
                    result));

            return result;
        }
    }

    private sealed class HandlerProbe
    {
        private readonly TaskCompletionSource<
            ProcessedDelivery> _processed =
                new(
                    TaskCreationOptions
                        .RunContinuationsAsynchronously);

        private int
            _invocationCount;

        public int InvocationCount =>
            Volatile.Read(
                ref _invocationCount);

        public void Record(
            StockDepletedIntegrationEvent integrationEvent,
            string scopedCorrelationId)
        {
            Interlocked.Increment(
                ref _invocationCount);

            _processed.TrySetResult(
                new ProcessedDelivery(
                    integrationEvent,
                    scopedCorrelationId));
        }

        public Task<ProcessedDelivery> WaitAsync(
            TimeSpan timeout)
        {
            return _processed
                .Task
                .WaitAsync(
                    timeout);
        }
    }

    private sealed class FailureProbe
    {
        private readonly ConcurrentQueue<string>
            _correlationIds =
                new();

        private int
            _invocationCount;

        public int InvocationCount =>
            Volatile.Read(
                ref _invocationCount);

        public IReadOnlyCollection<string> CorrelationIds =>
            _correlationIds.ToArray();

        public void Record(
            string correlationId)
        {
            Interlocked.Increment(
                ref _invocationCount);

            _correlationIds.Enqueue(
                correlationId);
        }
    }

    private sealed class ProcessingResultProbe
    {
        private readonly TaskCompletionSource<
            ProcessingObservation> _firstProcessing =
                new(
                    TaskCreationOptions
                        .RunContinuationsAsynchronously);

        private readonly TaskCompletionSource<
            ProcessingObservation> _secondProcessing =
                new(
                    TaskCreationOptions
                        .RunContinuationsAsynchronously);

        private int
            _processingCount;

        public void Record(
            ProcessingObservation observation)
        {
            var processingNumber =
                Interlocked.Increment(
                    ref _processingCount);

            switch (processingNumber)
            {
                case 1:
                    _firstProcessing.TrySetResult(
                        observation);
                    break;

                case 2:
                    _secondProcessing.TrySetResult(
                        observation);
                    break;
            }
        }

        public Task<ProcessingObservation> WaitForFirstAsync(
            TimeSpan timeout)
        {
            return _firstProcessing
                .Task
                .WaitAsync(
                    timeout);
        }

        public Task<ProcessingObservation> WaitForSecondAsync(
            TimeSpan timeout)
        {
            return _secondProcessing
                .Task
                .WaitAsync(
                    timeout);
        }
    }

    private sealed record ProcessedDelivery(
        StockDepletedIntegrationEvent IntegrationEvent,
        string ScopedCorrelationId);

    private sealed record ProcessingObservation(
        Guid EventId,
        IntegrationEventProcessingResult Result);

    private sealed class RecordingDeadLetterPublisher
        : IDeadLetterPublisher
    {
        private readonly TaskCompletionSource<
            DeadLetterMessage> _published =
                new(
                    TaskCreationOptions
                        .RunContinuationsAsynchronously);

        private int
            _invocationCount;

        public int InvocationCount =>
            Volatile.Read(
                ref _invocationCount);

        public DeadLetterMessage?
            LastMessage
        { get; private set; }

        public Task PublishAsync(
            DeadLetterMessage message,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(
                message);

            LastMessage =
                message;

            Interlocked.Increment(
                ref _invocationCount);

            _published.TrySetResult(
                message);

            return Task.CompletedTask;
        }

        public Task<DeadLetterMessage> WaitAsync(
            TimeSpan timeout)
        {
            return _published
                .Task
                .WaitAsync(
                    timeout);
        }
    }

    private static async Task SeedCandidateProductsAsync(
        OutboxTestDatabase database,
        Guid depletedProductId,
        Guid highStockCandidateId,
        Guid lowStockCandidateId,
        Guid differentCategoryProductId)
    {
        var mouseCategory =
            ProductCategory.From(
                "Mouse");

        var keyboardCategory =
            ProductCategory.From(
                "Keyboard");

        var depletedId =
            ProductId.From(
                depletedProductId);

        var highStockId =
            ProductId.From(
                highStockCandidateId);

        var lowStockId =
            ProductId.From(
                lowStockCandidateId);

        var differentCategoryId =
            ProductId.From(
                differentCategoryProductId);

        await using var context =
            database.CreateContext();

        context.Products.AddRange(
            Product.Create(
                depletedId,
                ProductName.From(
                    "Depleted Mouse"),
                mouseCategory),
            Product.Create(
                highStockId,
                ProductName.From(
                    "High Stock Mouse"),
                mouseCategory),
            Product.Create(
                lowStockId,
                ProductName.From(
                    "Low Stock Mouse"),
                mouseCategory),
            Product.Create(
                differentCategoryId,
                ProductName.From(
                    "Mechanical Keyboard"),
                keyboardCategory));

        context.InventoryItems.AddRange(
            InventoryItem.Create(
                depletedId,
                StockQuantity.Zero),
            InventoryItem.Create(
                highStockId,
                StockQuantity.From(20)),
            InventoryItem.Create(
                lowStockId,
                StockQuantity.From(5)),
            InventoryItem.Create(
                differentCategoryId,
                StockQuantity.From(100)));

        await context.SaveChangesAsync();
    }

    private static ServiceProvider CreateRecommendationFallbackServiceProvider(
        OutboxTestDatabase database,
        KafkaTestTopic topic,
        FakeChatCompletionService fakeChatCompletionService)
    {
        var services =
            new ServiceCollection();

        services.AddLogging();

        services.AddSingleton<
            IChatCompletionService>(
            fakeChatCompletionService);

        services.AddScoped<
            ICorrelationContext,
            CorrelationContext>();

        services.AddDbContext<
            FlashSaleOrchestratorDbContext>(
            options =>
                options.UseSqlServer(
                    database.ConnectionString));

        services.AddSingleton<
            StockDepletedOutboxMessageMapper>();

        services.AddScoped<
            OutboxProcessor>();

        services.AddSingleton<
            IOptions<KafkaPublisherOptions>>(
            Options.Create(
                new KafkaPublisherOptions
                {
                    BootstrapServers =
                        topic.BootstrapServers,

                    StockDepletedTopic =
                        topic.Name
                }));

        services.AddSingleton<
            IEventPublisher,
            KafkaEventPublisher>();

        services.AddScoped<
            IAlternativeCandidateProvider,
            SqlAlternativeCandidateProvider>();

        services.AddScoped<
            SemanticKernelAlternativeRecommendationGenerator>();

        services.AddScoped<
            IUncachedAlternativeRecommendationGenerator>(
            serviceProvider =>
                serviceProvider.GetRequiredService<
                    SemanticKernelAlternativeRecommendationGenerator>());

        services.AddSingleton<
            ISemanticRecommendationEmbeddingGenerator,
            E2eSemanticRecommendationEmbeddingGenerator>();

        services.AddSingleton<
            ISemanticRecommendationCache,
            E2eCacheMissSemanticRecommendationCache>();

        services.AddSingleton(
            new SemanticRecommendationCacheProfile(
                "e2e-prompt-v1",
                "e2e-schema-v1",
                "e2e-cache-v1",
                "e2e-embedding-v1"));

        services.AddSingleton<
            SemanticRecommendationRepresentationBuilder>();

        services.AddScoped<
            CachedSemanticAlternativeRecommendationGenerator>();

        services.AddScoped<
            DeterministicAlternativeRecommendationGenerator>();

        services.AddScoped<
            IAlternativeRecommendationGenerator,
            ResilientAlternativeRecommendationGenerator>();

        services.AddScoped<
            IIntegrationEventHandler<
                StockDepletedIntegrationEvent>,
            StockDepletedIntegrationEventHandler>();

        services.AddScoped<
            IIntegrationEventProcessor<
                StockDepletedIntegrationEvent>,
            InboxIntegrationEventProcessor<
                StockDepletedIntegrationEvent>>();

        return services.BuildServiceProvider();
    }

    private static ServiceProvider CreateCandidateRetrievalServiceProvider(
        OutboxTestDatabase database,
        KafkaTestTopic topic,
        CandidateProbe candidateProbe)
    {
        var services =
            new ServiceCollection();

        services.AddLogging();

        services.AddSingleton(
            candidateProbe);

        services.AddScoped<
            ICorrelationContext,
            CorrelationContext>();

        services.AddDbContext<
            FlashSaleOrchestratorDbContext>(
            options =>
                options.UseSqlServer(
                    database.ConnectionString));

        services.AddSingleton<
            StockDepletedOutboxMessageMapper>();

        services.AddScoped<
            OutboxProcessor>();

        services.AddSingleton<
            IOptions<KafkaPublisherOptions>>(
            Options.Create(
                new KafkaPublisherOptions
                {
                    BootstrapServers =
                        topic.BootstrapServers,

                    StockDepletedTopic =
                        topic.Name
                }));

        services.AddSingleton<
            IEventPublisher,
            KafkaEventPublisher>();

        services.AddScoped<
            SqlAlternativeCandidateProvider>();

        services.AddScoped<
            IAlternativeCandidateProvider,
            RecordingAlternativeCandidateProvider>();

        services.AddScoped<
            IAlternativeRecommendationGenerator,
            NoOpAlternativeRecommendationGenerator>();

        services.AddScoped<
            IIntegrationEventHandler<
                StockDepletedIntegrationEvent>,
            StockDepletedIntegrationEventHandler>();

        services.AddScoped<
            IIntegrationEventProcessor<
                StockDepletedIntegrationEvent>,
            InboxIntegrationEventProcessor<
                StockDepletedIntegrationEvent>>();

        return services.BuildServiceProvider();
    }

    private sealed class RecordingAlternativeCandidateProvider
        : IAlternativeCandidateProvider
    {
        private readonly SqlAlternativeCandidateProvider
            _innerProvider;

        private readonly CandidateProbe
            _candidateProbe;

        public RecordingAlternativeCandidateProvider(
            SqlAlternativeCandidateProvider innerProvider,
            CandidateProbe candidateProbe)
        {
            ArgumentNullException.ThrowIfNull(
                innerProvider);

            ArgumentNullException.ThrowIfNull(
                candidateProbe);

            _innerProvider =
                innerProvider;

            _candidateProbe =
                candidateProbe;
        }

        public async Task<AlternativeCandidateSet?> GetCandidateSetAsync(
            Guid depletedProductId,
            int limit,
            CancellationToken cancellationToken = default)
        {
            var candidateSet =
                await _innerProvider.GetCandidateSetAsync(
                    depletedProductId,
                    limit,
                    cancellationToken);

            _candidateProbe.Record(
                candidateSet?.Candidates
                ?? Array.Empty<AlternativeCandidate>());

            return candidateSet;
        }
    }

    private sealed class NoOpAlternativeRecommendationGenerator
        : IAlternativeRecommendationGenerator
    {
        public Task<AlternativeRecommendationResult> GenerateAsync(
            AlternativeRecommendationRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(
                request);

            return Task.FromResult(
                new AlternativeRecommendationResult(
                    []));
        }
    }

    private sealed class E2eSemanticRecommendationEmbeddingGenerator
        : ISemanticRecommendationEmbeddingGenerator
    {
        public Task<SemanticRecommendationEmbedding> GenerateAsync(
            string text,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(
                text);

            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(
                new SemanticRecommendationEmbedding(
                    new float[]
                    {
                        0.1f,
                        0.2f,
                        0.3f
                    }));
        }
    }

    private sealed class E2eCacheMissSemanticRecommendationCache
        : ISemanticRecommendationCache
    {
        public Task<SemanticRecommendationCacheMatch?> FindAsync(
            SemanticRecommendationCacheLookup lookup,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(
                lookup);

            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult<
                SemanticRecommendationCacheMatch?>(
                null);
        }

        public Task StoreAsync(
            SemanticRecommendationCacheEntry entry,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(
                entry);

            cancellationToken.ThrowIfCancellationRequested();

            return Task.CompletedTask;
        }

        public Task RemoveAsync(
            string entryId,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(
                entryId);

            cancellationToken.ThrowIfCancellationRequested();

            return Task.CompletedTask;
        }
    }

    private sealed class CandidateProbe
    {
        private readonly TaskCompletionSource<
            IReadOnlyList<AlternativeCandidate>> _retrieved =
                new(
                    TaskCreationOptions
                        .RunContinuationsAsynchronously);

        private int
            _invocationCount;

        public int InvocationCount =>
            Volatile.Read(
                ref _invocationCount);

        public void Record(
            IReadOnlyList<AlternativeCandidate> candidates)
        {
            ArgumentNullException.ThrowIfNull(
                candidates);

            Interlocked.Increment(
                ref _invocationCount);

            _retrieved.TrySetResult(
                candidates);
        }

        public Task<IReadOnlyList<AlternativeCandidate>> WaitAsync(
            TimeSpan timeout)
        {
            return _retrieved
                .Task
                .WaitAsync(
                    timeout);
        }
    }
}