using System.Diagnostics;
using System.Text.Json;
using Confluent.Kafka;
using ECommerce.FlashSaleOrchestrator.Api.BackgroundServices;
using ECommerce.FlashSaleOrchestrator.Application.Abstractions.Messaging;
using ECommerce.FlashSaleOrchestrator.Application.IntegrationEvents.Inventory;
using ECommerce.FlashSaleOrchestrator.Domain.Inventory.Events;
using ECommerce.FlashSaleOrchestrator.Infrastructure.Messaging.Kafka;
using ECommerce.FlashSaleOrchestrator.Infrastructure.Persistence;
using ECommerce.FlashSaleOrchestrator.Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ECommerce.FlashSaleOrchestrator.Infrastructure.IntegrationTests.Outbox;

public sealed class OutboxPublisherIntegrationTests
{
    [Fact]
    public async Task Worker_ShouldPublishPendingMessage_ToRedpanda_AndMarkItProcessed()
    {
        await using var database =
            await OutboxTestDatabase.CreateAsync();

        await using var topic =
            await KafkaTestTopic.CreateAsync();

        var eventId =
            Guid.NewGuid();

        var productId =
            Guid.NewGuid();

        var occurredAtUtc =
            new DateTime(
                2026,
                8,
                17,
                12,
                30,
                0,
                DateTimeKind.Utc);

        await SeedPendingMessageAsync(
            database,
            eventId,
            productId,
            occurredAtUtc);

        var services =
            new ServiceCollection();

        services.AddLogging();

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

        services.AddSingleton<
            IOptions<OutboxPublisherOptions>>(
            Options.Create(
                new OutboxPublisherOptions
                {
                    BatchSize =
                        10,

                    PollingInterval =
                        TimeSpan.FromMilliseconds(
                            100)
                }));

        services.AddSingleton<
            OutboxPublisherWorker>();

        await using var serviceProvider =
            services.BuildServiceProvider();

        var worker =
            serviceProvider
                .GetRequiredService<
                    OutboxPublisherWorker>();

        await worker.StartAsync(
            CancellationToken.None);

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

            // Give the worker several more polling cycles.
            // A processed message must not be selected again.
            await Task.Delay(
                TimeSpan.FromMilliseconds(400));
        }
        finally
        {
            using var stopCancellation =
                new CancellationTokenSource(
                    TimeSpan.FromSeconds(5));

            await worker.StopAsync(
                stopCancellation.Token);
        }

        using var consumer =
            topic.CreateConsumer();

        var consumedMessage =
            consumer.Consume(
                TimeSpan.FromSeconds(10));

        Assert.NotNull(
            consumedMessage);

        Assert.Equal(
            eventId.ToString("D"),
            consumedMessage!.Message.Key);

        using var payload =
            JsonDocument.Parse(
                consumedMessage.Message.Value);

        var root =
            payload.RootElement;

        Assert.Equal(
            eventId,
            root.GetProperty(
                    "eventId")
                .GetGuid());

        Assert.Equal(
            StockDepletedIntegrationEvent.EventTypeName,
            root.GetProperty(
                    "eventType")
                .GetString());

        Assert.Equal(
            productId,
            root.GetProperty(
                    "productId")
                .GetGuid());

        Assert.Equal(
            occurredAtUtc,
            root.GetProperty(
                    "occurredAtUtc")
                .GetDateTime());

        var duplicateMessage =
            consumer.Consume(
                TimeSpan.FromSeconds(1));

        Assert.Null(
            duplicateMessage);

        consumer.Close();

        await using (
            var verificationContext =
                database.CreateContext())
        {
            var persistedMessage =
                await verificationContext
                    .OutboxMessages
                    .AsNoTracking()
                    .SingleAsync(
                        message =>
                            message.Id == eventId);

            Assert.NotNull(
                persistedMessage.ProcessedAtUtc);
        }

        using var scope =
            serviceProvider.CreateScope();

        var processor =
            scope.ServiceProvider
                .GetRequiredService<
                    OutboxProcessor>();

        var processedAgain =
            await processor.ProcessPendingAsync(
                10);

        Assert.Equal(
            0,
            processedAgain);
    }

    [Fact]
    public async Task Processor_ShouldLeaveMessagePending_WhenPublisherFails()
    {
        await using var database =
            await OutboxTestDatabase.CreateAsync();

        var eventId =
            Guid.NewGuid();

        var productId =
            Guid.NewGuid();

        var occurredAtUtc =
            new DateTime(
                2026,
                8,
                17,
                13,
                0,
                0,
                DateTimeKind.Utc);

        await SeedPendingMessageAsync(
            database,
            eventId,
            productId,
            occurredAtUtc);

        await using var context =
            database.CreateContext();

        var processor =
            new OutboxProcessor(
                context,
                new StockDepletedOutboxMessageMapper(),
                new ThrowingEventPublisher());

        await Assert.ThrowsAsync<
            InvalidOperationException>(
            () =>
                processor.ProcessPendingAsync(
                    10));

        await using var verificationContext =
            database.CreateContext();

        var persistedMessage =
            await verificationContext
                .OutboxMessages
                .AsNoTracking()
                .SingleAsync(
                    message =>
                        message.Id == eventId);

        Assert.Null(
            persistedMessage.ProcessedAtUtc);
    }

    private static async Task SeedPendingMessageAsync(
        OutboxTestDatabase database,
        Guid eventId,
        Guid productId,
        DateTime occurredAtUtc)
    {
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
                payload);

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
                TimeSpan.FromMilliseconds(100));
        }

        throw new TimeoutException(
            $"Condition was not satisfied within {timeout}.");
    }

    private sealed class ThrowingEventPublisher
        : IEventPublisher
    {
        public Task PublishAsync<TEvent>(
            TEvent integrationEvent,
            CancellationToken cancellationToken = default)
            where TEvent : class
        {
            throw new InvalidOperationException(
                "Simulated broker failure.");
        }
    }
}