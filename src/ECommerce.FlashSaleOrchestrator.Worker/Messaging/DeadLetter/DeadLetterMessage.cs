namespace ECommerce.FlashSaleOrchestrator.Worker.Messaging.DeadLetter;

public sealed record DeadLetterMessage(
    Guid? EventId,
    string? EventType,
    string? OriginalKey,
    string OriginalTopic,
    int OriginalPartition,
    long OriginalOffset,
    string Payload,
    string ErrorType,
    string ErrorMessage,
    DateTime FailedAtUtc);