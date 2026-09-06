using ECommerce.FlashSaleOrchestrator.Application.AlternativeCandidates;

namespace ECommerce.FlashSaleOrchestrator.Application.Abstractions.AlternativeCandidates;

public interface IAlternativeCandidateProvider
{
    Task<AlternativeCandidateSet?> GetCandidateSetAsync(
        Guid depletedProductId,
        int limit,
        CancellationToken cancellationToken = default);
}