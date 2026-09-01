using ECommerce.FlashSaleOrchestrator.Application.AlternativeCandidates;

namespace ECommerce.FlashSaleOrchestrator.Application.AlternativeRecommendations;

public sealed record AlternativeRecommendationRequest(
    string CorrelationId,
    DepletedProductContext DepletedProduct,
    IReadOnlyList<AlternativeCandidate> Candidates);