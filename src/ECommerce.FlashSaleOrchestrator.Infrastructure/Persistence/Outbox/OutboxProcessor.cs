using System.Diagnostics;
using ECommerce.FlashSaleOrchestrator.Application
    .Abstractions.Messaging;
using ECommerce.FlashSaleOrchestrator.Infrastructure
    .Observability;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ECommerce.FlashSaleOrchestrator.Infrastructure
    .Persistence.Outbox;

public sealed class OutboxProcessor
{
    private readonly FlashSaleOrchestratorDbContext
        _dbContext;

    private readonly StockDepletedOutboxMessageMapper
        _mapper;

    private readonly IEventPublisher
        _eventPublisher;

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

        var pendingQuery =
            _dbContext.OutboxMessages
                .Where(
                    message =>
                        message.ProcessedAtUtc == null
                        && message.Type ==
                            _mapper.SourceEventType);

        var pendingCount =
            await pendingQuery.CountAsync(
                cancellationToken);

        InfrastructureMetrics.OutboxPending.Record(
            pendingCount);

        var pendingMessages =
            await pendingQuery
                .OrderBy(
                    message =>
                        message.OccurredAtUtc)
                .ThenBy(
                    message =>
                        message.Id)
                .Take(
                    batchSize)
                .ToListAsync(
                    cancellationToken);

        var processedCount =
            0;

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

            var metricStage =
                "publish";

            try
            {
                await _eventPublisher.PublishAsync(
                    integrationEvent,
                    cancellationToken);

                InfrastructureMetrics.OutboxPublished.Add(
                    1);

                stage =
                    "PersistProcessed";

                metricStage =
                    "persist_processed";

                outboxMessage.MarkProcessed(
                    DateTime.UtcNow);

                await _dbContext.SaveChangesAsync(
                    cancellationToken);

                processedCount++;

                pendingCount =
                    Math.Max(
                        0,
                        pendingCount - 1);

                InfrastructureMetrics.OutboxPending.Record(
                    pendingCount);

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
                InfrastructureMetrics.OutboxFailures.Add(
                    1,
                    new KeyValuePair<string, object?>(
                        "stage",
                        metricStage));

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