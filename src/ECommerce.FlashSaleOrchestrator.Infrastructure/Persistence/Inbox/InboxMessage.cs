namespace ECommerce.FlashSaleOrchestrator.Infrastructure.Persistence.Inbox;

public sealed class InboxMessage
{
    private InboxMessage()
    {
    }

    public InboxMessage(
        Guid id,
        DateTime occurredAtUtc,
        string type)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "Inbox message id cannot be empty.",
                nameof(id));
        }

        if (occurredAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException(
                "Occurred timestamp must be UTC.",
                nameof(occurredAtUtc));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(
            type);

        Id =
            id;

        OccurredAtUtc =
            occurredAtUtc;

        Type =
            type;
    }

    public Guid Id { get; private set; }

    public DateTime OccurredAtUtc { get; private set; }

    public string Type { get; private set; } =
        null!;

    public DateTime? ProcessedAtUtc { get; private set; }

    public void MarkProcessed(
        DateTime processedAtUtc)
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
                "Inbox message has already been processed.");
        }

        ProcessedAtUtc =
            processedAtUtc;
    }
}