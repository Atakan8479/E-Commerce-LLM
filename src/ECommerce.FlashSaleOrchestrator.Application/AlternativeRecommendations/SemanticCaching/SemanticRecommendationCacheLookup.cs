namespace ECommerce.FlashSaleOrchestrator.Application
    .AlternativeRecommendations.SemanticCaching;

public sealed record SemanticRecommendationCacheLookup(
    ReadOnlyMemory<float> Embedding,
    SemanticRecommendationCacheCompatibility Compatibility);