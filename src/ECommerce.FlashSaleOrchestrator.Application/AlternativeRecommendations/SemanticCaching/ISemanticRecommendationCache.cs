namespace ECommerce.FlashSaleOrchestrator.Application
    .AlternativeRecommendations.SemanticCaching;

public interface ISemanticRecommendationCache
{
    Task<SemanticRecommendationCacheMatch?> FindAsync(
        SemanticRecommendationCacheLookup lookup,
        CancellationToken cancellationToken = default);

    Task StoreAsync(
        SemanticRecommendationCacheEntry entry,
        CancellationToken cancellationToken = default);

    Task RemoveAsync(
        string entryId,
        CancellationToken cancellationToken = default);
}