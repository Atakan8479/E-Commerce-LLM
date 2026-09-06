namespace ECommerce.FlashSaleOrchestrator.Application
    .AlternativeRecommendations.SemanticCaching;

public sealed record SemanticRecommendationCacheMatch(
    string EntryId,
    double SimilarityScore,
    AlternativeRecommendationResult Result);