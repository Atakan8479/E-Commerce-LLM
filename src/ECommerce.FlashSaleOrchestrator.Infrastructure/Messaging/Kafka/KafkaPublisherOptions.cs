namespace ECommerce.FlashSaleOrchestrator.Infrastructure.Messaging.Kafka;

public sealed class KafkaPublisherOptions
{
    public const string SectionName =
        "Kafka";

    public string BootstrapServers { get; set; } =
        string.Empty;

    public string StockDepletedTopic { get; set; } =
        string.Empty;
}