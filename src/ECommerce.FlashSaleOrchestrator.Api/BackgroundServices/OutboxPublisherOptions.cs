namespace ECommerce.FlashSaleOrchestrator.Api.BackgroundServices;

public sealed class OutboxPublisherOptions
{
    public const string SectionName =
        "OutboxPublisher";

    public int BatchSize { get; init; } = 50;

    public TimeSpan PollingInterval { get; init; } =
        TimeSpan.FromSeconds(5);
}