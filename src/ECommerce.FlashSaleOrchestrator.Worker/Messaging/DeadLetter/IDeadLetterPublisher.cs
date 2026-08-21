namespace ECommerce.FlashSaleOrchestrator.Worker.Messaging.DeadLetter;

public interface IDeadLetterPublisher
{
    Task PublishAsync(
        DeadLetterMessage message,
        CancellationToken cancellationToken = default);
}