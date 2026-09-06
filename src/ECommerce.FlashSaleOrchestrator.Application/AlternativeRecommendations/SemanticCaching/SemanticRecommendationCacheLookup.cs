namespace ECommerce.FlashSaleOrchestrator.Application
    .AlternativeRecommendations.SemanticCaching;

public sealed record SemanticRecommendationCacheLookup(
    SemanticRecommendationEmbedding Embedding,
    SemanticRecommendationCacheCompatibility Compatibility);