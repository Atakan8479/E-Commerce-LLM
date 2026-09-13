using ECommerce.FlashSaleOrchestrator.Application
    .AlternativeRecommendations;
using ECommerce.FlashSaleOrchestrator.Infrastructure
    .AI.SemanticCaching;
using Microsoft.Extensions.Logging;

namespace ECommerce.FlashSaleOrchestrator.Infrastructure.AI;

internal sealed class
    ResilientAlternativeRecommendationGenerator
    : IAlternativeRecommendationGenerator
{
    private const int MaxAttempts =
        2;

    private static readonly TimeSpan RetryDelay =
        TimeSpan.FromMilliseconds(
            250);

    private readonly
        CachedSemanticAlternativeRecommendationGenerator
        _primaryGenerator;

    private readonly
        DeterministicAlternativeRecommendationGenerator
        _fallbackGenerator;

    private readonly
        ILogger<
            ResilientAlternativeRecommendationGenerator>
        _logger;

    public ResilientAlternativeRecommendationGenerator(
        CachedSemanticAlternativeRecommendationGenerator
            primaryGenerator,
        DeterministicAlternativeRecommendationGenerator
            fallbackGenerator,
        ILogger<
            ResilientAlternativeRecommendationGenerator>
            logger)
    {
        ArgumentNullException.ThrowIfNull(
            primaryGenerator);

        ArgumentNullException.ThrowIfNull(
            fallbackGenerator);

        ArgumentNullException.ThrowIfNull(
            logger);

        _primaryGenerator =
            primaryGenerator;

        _fallbackGenerator =
            fallbackGenerator;

        _logger =
            logger;
    }

    public async Task<AlternativeRecommendationResult>
        GenerateAsync(
            AlternativeRecommendationRequest request,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            request);

        if (request.Candidates.Count == 0)
        {
            return await _fallbackGenerator.GenerateAsync(
                request,
                cancellationToken);
        }

        for (var attempt = 1;
             attempt <= MaxAttempts;
             attempt++)
        {
            try
            {
                return await _primaryGenerator.GenerateAsync(
                    request,
                    cancellationToken);
            }
            catch (OperationCanceledException)
                when (
                    cancellationToken
                        .IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
                when (attempt < MaxAttempts)
            {
                _logger.LogWarning(
                    exception,
                    "Alternative recommendation generation failed. " +
                    "LLM retry scheduled. " +
                    "CorrelationId: {CorrelationId}, " +
                    "DepletedProductId: {DepletedProductId}, " +
                    "Attempt: {Attempt}, " +
                    "MaxAttempts: {MaxAttempts}",
                    request.CorrelationId,
                    request.DepletedProduct.ProductId,
                    attempt,
                    MaxAttempts);

                await Task.Delay(
                    RetryDelay,
                    cancellationToken);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Alternative recommendation generation exhausted " +
                    "LLM attempts. Deterministic fallback will be used. " +
                    "CorrelationId: {CorrelationId}, " +
                    "DepletedProductId: {DepletedProductId}, " +
                    "Attempts: {Attempts}",
                    request.CorrelationId,
                    request.DepletedProduct.ProductId,
                    MaxAttempts);
            }
        }

        return await _fallbackGenerator.GenerateAsync(
            request,
            cancellationToken);
    }
}