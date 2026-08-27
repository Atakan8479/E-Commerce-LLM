namespace ECommerce.FlashSaleOrchestrator.Worker.Resilience;

public sealed class EventProcessingRetryOptions
{
    public const string SectionName =
        "EventProcessingRetry";

    public int MaxAttempts { get; set; } =
        3;

    public TimeSpan InitialDelay { get; set; } =
        TimeSpan.FromMilliseconds(500);
}