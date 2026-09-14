using ECommerce.FlashSaleOrchestrator.Application
    .AlternativeRecommendations;

namespace ECommerce.FlashSaleOrchestrator.Infrastructure
    .Persistence.AlternativeRecommendations;

public sealed class AlternativeRecommendationPlanRecord
{
    private AlternativeRecommendationPlanRecord()
    {
    }

    public AlternativeRecommendationPlanRecord(
        Guid eventId,
        Guid originalProductId,
        string payloadJson,
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

        ArgumentException.ThrowIfNullOrWhiteSpace(
            payloadJson);

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

        PayloadJson =
            payloadJson;

        Source =
            source;

        CorrelationId =
            correlationId;

        CreatedAtUtc =
            createdAtUtc;
    }

    public Guid EventId { get; private set; }

    public Guid OriginalProductId { get; private set; }

    public string PayloadJson { get; private set; } =
        null!;

    public AlternativeRecommendationSource Source
    {
        get;
        private set;
    }

    public string CorrelationId { get; private set; } =
        null!;

    public DateTime CreatedAtUtc { get; private set; }
}