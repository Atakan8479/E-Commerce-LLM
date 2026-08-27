using ECommerce.FlashSaleOrchestrator.Application.Abstractions.Messaging;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ECommerce.FlashSaleOrchestrator.Infrastructure.Persistence.Inbox;

public sealed class InboxIntegrationEventProcessor<TEvent>
    : IIntegrationEventProcessor<TEvent>
    where TEvent : class, IIntegrationEvent
{
    private readonly FlashSaleOrchestratorDbContext
        _dbContext;

    private readonly IIntegrationEventHandler<TEvent>
        _handler;

    private readonly ILogger<
        InboxIntegrationEventProcessor<TEvent>>
        _logger;

    public InboxIntegrationEventProcessor(
        FlashSaleOrchestratorDbContext dbContext,
        IIntegrationEventHandler<TEvent> handler,
        ILogger<InboxIntegrationEventProcessor<TEvent>> logger)
    {
        ArgumentNullException.ThrowIfNull(
            dbContext);

        ArgumentNullException.ThrowIfNull(
            handler);

        ArgumentNullException.ThrowIfNull(
            logger);

        _dbContext =
            dbContext;

        _handler =
            handler;

        _logger =
            logger;
    }

    public async Task<IntegrationEventProcessingResult> ProcessAsync(
        TEvent integrationEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            integrationEvent);

        var alreadyProcessed =
            await _dbContext
                .InboxMessages
                .AsNoTracking()
                .AnyAsync(
                    inboxMessage =>
                        inboxMessage.Id ==
                        integrationEvent.EventId,
                    cancellationToken);

        if (alreadyProcessed)
        {
            _logger.LogInformation(
                "Integration event was already processed. EventId: {EventId}, EventType: {EventType}",
                integrationEvent.EventId,
                integrationEvent.EventType);

            return IntegrationEventProcessingResult
                .AlreadyProcessed;
        }

        await using var transaction =
            await _dbContext
                .Database
                .BeginTransactionAsync(
                    cancellationToken);

        var inboxMessage =
            new InboxMessage(
                integrationEvent.EventId,
                integrationEvent.OccurredAtUtc,
                integrationEvent.EventType);

        _dbContext.InboxMessages.Add(
            inboxMessage);

        try
        {
            await _dbContext.SaveChangesAsync(
                cancellationToken);
        }
        catch (DbUpdateException exception)
            when (IsDuplicateKey(exception))
        {
            await transaction.RollbackAsync(
                cancellationToken);

            _dbContext.Entry(
                    inboxMessage)
                .State =
                EntityState.Detached;

            _logger.LogInformation(
                "Duplicate integration event detected. EventId: {EventId}, EventType: {EventType}",
                integrationEvent.EventId,
                integrationEvent.EventType);

            return IntegrationEventProcessingResult
                .AlreadyProcessed;
        }

        await _handler.HandleAsync(
            integrationEvent,
            cancellationToken);

        inboxMessage.MarkProcessed(
            DateTime.UtcNow);

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        await transaction.CommitAsync(
            cancellationToken);

        _logger.LogInformation(
            "Integration event processed through inbox. EventId: {EventId}, EventType: {EventType}",
            integrationEvent.EventId,
            integrationEvent.EventType);

        return IntegrationEventProcessingResult
            .Processed;
    }

    private static bool IsDuplicateKey(
        DbUpdateException exception)
    {
        return exception.InnerException
            is SqlException sqlException
            && (
                sqlException.Number == 2601
                || sqlException.Number == 2627
            );
    }
}