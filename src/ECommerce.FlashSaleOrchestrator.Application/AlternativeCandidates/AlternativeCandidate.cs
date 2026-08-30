namespace ECommerce.FlashSaleOrchestrator.Application.AlternativeCandidates;

public sealed record AlternativeCandidate(
    Guid ProductId,
    string Name,
    string Category,
    int AvailableQuantity);