using ECommerce.FlashSaleOrchestrator.Application.Abstractions.Messaging;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.FlashSaleOrchestrator.Infrastructure.Persistence.Outbox;

public sealed class OutboxProcessor
{
    private readonly FlashSaleOrchestratorDbContext _dbContext;
    private readonly StockDepletedOutboxMessageMapper _mapper;
    private readonly IEventPublisher _eventPublisher;

    public OutboxProcessor(
        FlashSaleOrchestratorDbContext dbContext,
        StockDepletedOutboxMessageMapper mapper,
        IEventPublisher eventPublisher)
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

            await _eventPublisher.PublishAsync(
                integrationEvent,
                cancellationToken);

            outboxMessage.MarkProcessed(
                DateTime.UtcNow);

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            processedCount++;
        }

        return processedCount;
    }
}