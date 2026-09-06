namespace ECommerce.FlashSaleOrchestrator.Application.AlternativeCandidates;

public sealed record AlternativeCandidateSet(
    DepletedProductContext DepletedProduct,
    IReadOnlyList<AlternativeCandidate> Candidates);

public sealed record DepletedProductContext(
    Guid ProductId,
    string Name,
    string Category);