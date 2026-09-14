namespace ECommerce.FlashSaleOrchestrator.Application
    .AlternativeRecommendations;

public sealed record AlternativeRecommendationPlan
{
    public AlternativeRecommendationPlan(
        Guid eventId,
        Guid originalProductId,
        AlternativeRecommendationResult result,
        AlternativeRecommendationSource source,
        string correlationId,
        DateTime createdAtUtc)
    {
        if (eventId == Guid.Empty)
        {
            throw new ArgumentException(
                "Event identifier cannot be empty.",
                nameof(eventId));
        }

        if (originalProductId == Guid.Empty)
        {
            throw new ArgumentException(
                "Original product identifier cannot be empty.",
                nameof(originalProductId));
        }

        ArgumentNullException.ThrowIfNull(
            result);

        if (!Enum.IsDefined(
                source))
        {
            throw new ArgumentOutOfRangeException(
                nameof(source),
                source,
                "Recommendation source is not supported.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(
            correlationId);

        if (createdAtUtc.Kind !=
            DateTimeKind.Utc)
        {
            throw new ArgumentException(
                "Created timestamp must be UTC.",
                nameof(createdAtUtc));
        }

        EventId =
            eventId;

        OriginalProductId =
            originalProductId;

        Result =
            result;

        Source =
            source;

        CorrelationId =
            correlationId;

        CreatedAtUtc =
            createdAtUtc;
    }

    public Guid EventId { get; }

    public Guid OriginalProductId { get; }

    public AlternativeRecommendationResult Result { get; }

    public AlternativeRecommendationSource Source { get; }

    public string CorrelationId { get; }

    public DateTime CreatedAtUtc { get; }
}