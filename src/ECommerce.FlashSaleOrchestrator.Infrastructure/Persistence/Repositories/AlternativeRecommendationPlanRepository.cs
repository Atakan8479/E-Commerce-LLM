using System.Text.Json;
using ECommerce.FlashSaleOrchestrator.Application
    .Abstractions.Persistence;
using ECommerce.FlashSaleOrchestrator.Application
    .AlternativeRecommendations;
using ECommerce.FlashSaleOrchestrator.Infrastructure
    .Persistence.AlternativeRecommendations;
using Microsoft.EntityFrameworkCore;

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

    public async Task<
        IReadOnlyList<AlternativeRecommendationPlan>>
        ListByCorrelationIdAsync(
            string correlationId,
            CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            correlationId);

        var records =
            await _dbContext
                .AlternativeRecommendationPlans
                .AsNoTracking()
                .Where(
                    plan =>
                        plan.CorrelationId ==
                        correlationId)
                .OrderBy(
                    plan =>
                        plan.CreatedAtUtc)
                .ThenBy(
                    plan =>
                        plan.EventId)
                .ToArrayAsync(
                    cancellationToken);

        return records
            .Select(
                MapToPlan)
            .ToArray();
    }

    private static AlternativeRecommendationPlan
        MapToPlan(
            AlternativeRecommendationPlanRecord record)
    {
        var result =
            JsonSerializer.Deserialize<
                AlternativeRecommendationResult>(
                record.PayloadJson,
                JsonOptions)
            ?? throw new InvalidOperationException(
                $"Recommendation payload for event " +
                $"'{record.EventId}' could not be deserialized.");

        var createdAtUtc =
            record.CreatedAtUtc.Kind ==
            DateTimeKind.Utc
                ? record.CreatedAtUtc
                : DateTime.SpecifyKind(
                    record.CreatedAtUtc,
                    DateTimeKind.Utc);

        return new AlternativeRecommendationPlan(
            record.EventId,
            record.OriginalProductId,
            result,
            record.Source,
            record.CorrelationId,
            createdAtUtc);
    }
}