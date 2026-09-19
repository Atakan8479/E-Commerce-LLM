using ECommerce.FlashSaleOrchestrator.Application.Abstractions.Messaging;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace ECommerce.FlashSaleOrchestrator.Infrastructure.Persistence.Outbox;

public sealed class OutboxProcessor
{
    private readonly FlashSaleOrchestratorDbContext _dbContext;
    private readonly StockDepletedOutboxMessageMapper _mapper;
    private readonly IEventPublisher _eventPublisher;

    private readonly ILogger<OutboxProcessor>
    _logger;

    public OutboxProcessor(
        FlashSaleOrchestratorDbContext dbContext,
        StockDepletedOutboxMessageMapper mapper,
        IEventPublisher eventPublisher,
        ILogger<OutboxProcessor> logger)
    {
        _dbContext =
            dbContext
            ?? throw new ArgumentNullException(
                nameof(dbContext));

        _mapper =
            mapper
            ?? throw new ArgumentNullException(
                nameof(mapper));

        _eventPublisher =
            eventPublisher
            ?? throw new ArgumentNullException(
                nameof(eventPublisher));

        _logger =
            logger
            ?? throw new ArgumentNullException(
                nameof(logger));
    }

    public async Task<int> ProcessPendingAsync(
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        if (batchSize <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(batchSize),
                batchSize,
                "Batch size must be greater than zero.");
        }

        var pendingMessages =
            await _dbContext.OutboxMessages
                .Where(
                    message =>
                        message.ProcessedAtUtc == null
                        && message.Type == _mapper.SourceEventType)
                .OrderBy(
                    message => message.OccurredAtUtc)
                .ThenBy(
                    message => message.Id)
                .Take(batchSize)
                .ToListAsync(
                    cancellationToken);

        var processedCount = 0;

        foreach (var outboxMessage in pendingMessages)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var integrationEvent =
                _mapper.Map(
                    outboxMessage);

            using var logScope =
                _logger.BeginScope(
                    new Dictionary<string, object>
                    {
                        ["CorrelationId"] =
                            integrationEvent.CorrelationId,

                        ["EventId"] =
                            integrationEvent.EventId,

                        ["ProductId"] =
                            integrationEvent.ProductId,

                        ["EventType"] =
                            integrationEvent.EventType
                    });

            var processingStartedAt =
                Stopwatch.GetTimestamp();

            var stage =
                "Publish";

            try
            {
                await _eventPublisher.PublishAsync(
                    integrationEvent,
                    cancellationToken);

                stage =
                    "PersistProcessed";

                outboxMessage.MarkProcessed(
                    DateTime.UtcNow);

                await _dbContext.SaveChangesAsync(
                    cancellationToken);

                processedCount++;

                var durationMs =
                    Stopwatch.GetElapsedTime(
                            processingStartedAt)
                        .TotalMilliseconds;

                _logger.LogInformation(
                    "Outbox message published and marked as processed. " +
                    "DurationMs: {DurationMs}",
                    durationMs);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                var durationMs =
                    Stopwatch.GetElapsedTime(
                            processingStartedAt)
                        .TotalMilliseconds;

                _logger.LogError(
                    exception,
                    "Outbox message processing failed. " +
                    "Stage: {Stage}, " +
                    "DurationMs: {DurationMs}",
                    stage,
                    durationMs);

                throw;
            }
        }

        return processedCount;
    }
}