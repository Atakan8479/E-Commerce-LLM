using System.Text.Json;
using ECommerce.FlashSaleOrchestrator.Application
    .Abstractions.Persistence;
using ECommerce.FlashSaleOrchestrator.Application
    .AlternativeRecommendations;
using ECommerce.FlashSaleOrchestrator.Infrastructure
    .Persistence.AlternativeRecommendations;

namespace ECommerce.FlashSaleOrchestrator.Infrastructure
    .Persistence.Repositories;

public sealed class AlternativeRecommendationPlanRepository
    : IAlternativeRecommendationPlanRepository
{
    private static readonly JsonSerializerOptions
        JsonOptions =
            new(
                JsonSerializerDefaults.Web);

    private readonly FlashSaleOrchestratorDbContext
        _dbContext;

    public AlternativeRecommendationPlanRepository(
        FlashSaleOrchestratorDbContext dbContext)
    {
        _dbContext =
            dbContext
            ?? throw new ArgumentNullException(
                nameof(dbContext));
    }

    public async Task AddAsync(
        AlternativeRecommendationPlan plan,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            plan);

        var payloadJson =
            JsonSerializer.Serialize(
                plan.Result,
                JsonOptions);

        var record =
            new AlternativeRecommendationPlanRecord(
                plan.EventId,
                plan.OriginalProductId,
                payloadJson,
                plan.Source,
                plan.CorrelationId,
                plan.CreatedAtUtc);

        await _dbContext
            .AlternativeRecommendationPlans
            .AddAsync(
                record,
                cancellationToken);
    }
}