using ECommerce.FlashSaleOrchestrator.Application.AlternativeCandidates;

namespace ECommerce.FlashSaleOrchestrator.Application.Abstractions.AlternativeCandidates;

public interface IAlternativeCandidateProvider
{
    Task<IReadOnlyList<AlternativeCandidate>> GetCandidatesAsync(
        Guid depletedProductId,
        int limit,
        CancellationToken cancellationToken = default);
}