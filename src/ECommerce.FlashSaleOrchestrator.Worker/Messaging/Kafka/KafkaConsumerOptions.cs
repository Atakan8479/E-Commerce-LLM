namespace ECommerce.FlashSaleOrchestrator.Worker.Messaging.Kafka;

public sealed class KafkaConsumerOptions
{
    public const string SectionName =
        "Kafka";

    public string BootstrapServers { get; set; } =
        string.Empty;

    public string StockDepletedTopic { get; set; } =
        string.Empty;

    public string ConsumerGroupId { get; set; } =
        string.Empty;
}