using ECommerce.FlashSaleOrchestrator.Application.Abstractions.Messaging;
using ECommerce.FlashSaleOrchestrator.Application.IntegrationEvents.Inventory;
using ECommerce.FlashSaleOrchestrator.Infrastructure.Persistence;
using ECommerce.FlashSaleOrchestrator.Infrastructure.Persistence.Inbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace ECommerce.FlashSaleOrchestrator.Infrastructure.IntegrationTests.Inbox;

public sealed class InboxIntegrationEventProcessorTests
{
    [Fact]
    public async Task ProcessAsync_ShouldProcessEventAndPersistInboxMessage_WhenEventIsNew()
    {
        await using var database =
            await InboxTestDatabase.CreateAsync();

        await using var context =
            database.CreateContext();

        var handler =
            new RecordingIntegrationEventHandler();

        var processor =
            CreateProcessor(
                context,
                handler);

        var integrationEvent =
            CreateIntegrationEvent();

        var result =
            await processor.ProcessAsync(
                integrationEvent);

        Assert.Equal(
            IntegrationEventProcessingResult.Processed,
            result);

        Assert.Equal(
            1,
            handler.InvocationCount);

        Assert.Equal(
            integrationEvent.EventId,
            handler.LastEvent?.EventId);

        await using var verificationContext =
            database.CreateContext();

        var inboxMessage =
            await verificationContext
                .InboxMessages
                .AsNoTracking()
                .SingleAsync(
                    message =>
                        message.Id ==
                        integrationEvent.EventId);

        Assert.Equal(
            integrationEvent.EventId,
            inboxMessage.Id);

        Assert.Equal(
            integrationEvent.OccurredAtUtc,
            inboxMessage.OccurredAtUtc);

        Assert.Equal(
            StockDepletedIntegrationEvent.EventTypeName,
            inboxMessage.Type);

        Assert.NotNull(
            inboxMessage.ProcessedAtUtc);
    }

    [Fact]
    public async Task ProcessAsync_ShouldReturnAlreadyProcessedAndNotInvokeHandlerAgain_WhenEventIdWasProcessed()
    {
        await using var database =
            await InboxTestDatabase.CreateAsync();

        await using var context =
            database.CreateContext();

        var handler =
            new RecordingIntegrationEventHandler();

        var processor =
            CreateProcessor(
                context,
                handler);

        var integrationEvent =
            CreateIntegrationEvent();

        var firstResult =
            await processor.ProcessAsync(
                integrationEvent);

        var secondResult =
            await processor.ProcessAsync(
                integrationEvent);

        Assert.Equal(
            IntegrationEventProcessingResult.Processed,
            firstResult);

        Assert.Equal(
            IntegrationEventProcessingResult.AlreadyProcessed,
            secondResult);

        Assert.Equal(
            1,
            handler.InvocationCount);

        await using var verificationContext =
            database.CreateContext();

        var inboxMessageCount =
            await verificationContext
                .InboxMessages
                .AsNoTracking()
                .CountAsync(
                    message =>
                        message.Id ==
                        integrationEvent.EventId);

        Assert.Equal(
            1,
            inboxMessageCount);
    }

    [Fact]
    public async Task ProcessAsync_ShouldRollbackInboxMessageAndAllowRetry_WhenHandlerFails()
    {
        await using var database =
            await InboxTestDatabase.CreateAsync();

        var integrationEvent =
            CreateIntegrationEvent();

        await using (var failingContext =
                     database.CreateContext())
        {
            var failingHandler =
                new RecordingIntegrationEventHandler(
                    shouldFail: true);

            var failingProcessor =
                CreateProcessor(
                    failingContext,
                    failingHandler);

            await Assert.ThrowsAsync<
                InvalidOperationException>(
                () =>
                    failingProcessor.ProcessAsync(
                        integrationEvent));

            Assert.Equal(
                1,
                failingHandler.InvocationCount);
        }

        await using (var verificationContext =
                     database.CreateContext())
        {
            var inboxMessageExists =
                await verificationContext
                    .InboxMessages
                    .AsNoTracking()
                    .AnyAsync(
                        message =>
                            message.Id ==
                            integrationEvent.EventId);

            Assert.False(
                inboxMessageExists);
        }

        await using (var retryContext =
                     database.CreateContext())
        {
            var retryHandler =
                new RecordingIntegrationEventHandler();

            var retryProcessor =
                CreateProcessor(
                    retryContext,
                    retryHandler);

            var retryResult =
                await retryProcessor.ProcessAsync(
                    integrationEvent);

            Assert.Equal(
                IntegrationEventProcessingResult.Processed,
                retryResult);

            Assert.Equal(
                1,
                retryHandler.InvocationCount);
        }

        await using var finalVerificationContext =
            database.CreateContext();

        var inboxMessage =
            await finalVerificationContext
                .InboxMessages
                .AsNoTracking()
                .SingleAsync(
                    message =>
                        message.Id ==
                        integrationEvent.EventId);

        Assert.NotNull(
            inboxMessage.ProcessedAtUtc);
    }

    private static InboxIntegrationEventProcessor<
        StockDepletedIntegrationEvent> CreateProcessor(
        FlashSaleOrchestratorDbContext context,
        IIntegrationEventHandler<
            StockDepletedIntegrationEvent> handler)
    {
        return new InboxIntegrationEventProcessor<
            StockDepletedIntegrationEvent>(
            context,
            handler,
            NullLogger<
                InboxIntegrationEventProcessor<
                    StockDepletedIntegrationEvent>>
                .Instance);
    }

    private static StockDepletedIntegrationEvent
        CreateIntegrationEvent()
    {
        return new StockDepletedIntegrationEvent(
            Guid.NewGuid(),
            DateTime.UtcNow,
            Guid.NewGuid());
    }

    private sealed class RecordingIntegrationEventHandler
        : IIntegrationEventHandler<
            StockDepletedIntegrationEvent>
    {
        private readonly bool _shouldFail;

        public RecordingIntegrationEventHandler(
            bool shouldFail = false)
        {
            _shouldFail =
                shouldFail;
        }

        public int InvocationCount { get; private set; }

        public StockDepletedIntegrationEvent?
            LastEvent { get; private set; }

        public Task HandleAsync(
            StockDepletedIntegrationEvent integrationEvent,
            CancellationToken cancellationToken = default)
        {
            InvocationCount++;

            LastEvent =
                integrationEvent;

            if (_shouldFail)
            {
                throw new InvalidOperationException(
                    "Simulated integration event handler failure.");
            }

            return Task.CompletedTask;
        }
    }
}