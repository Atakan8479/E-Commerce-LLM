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
}