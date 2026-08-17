namespace ECommerce.FlashSaleOrchestrator.Infrastructure.Persistence.Outbox;

public sealed class OutboxMessage
{
    private OutboxMessage()
    {
    }

    public OutboxMessage(
        Guid id,
        DateTime occurredAtUtc,
        string type,
        string payload)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "Outbox message id cannot be empty.",
                nameof(id));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(type);
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);

        Id = id;
        OccurredAtUtc = occurredAtUtc;
        Type = type;
        Payload = payload;
    }

    public Guid Id { get; private set; }

    public DateTime OccurredAtUtc { get; private set; }

    public string Type { get; private set; } = null!;

    public string Payload { get; private set; } = null!;

    public DateTime? ProcessedAtUtc { get; private set; }

    public void MarkProcessed(DateTime processedAtUtc)
    {
        if (processedAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException(
                "Processed timestamp must be UTC.",
                nameof(processedAtUtc));
        }

        if (ProcessedAtUtc.HasValue)
        {
            throw new InvalidOperationException(
                "Outbox message has already been processed.");
        }

        ProcessedAtUtc = processedAtUtc;
    }
}