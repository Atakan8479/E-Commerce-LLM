using ECommerce.FlashSaleOrchestrator.Application
    .AlternativeRecommendations;

namespace ECommerce.FlashSaleOrchestrator.Infrastructure.AI;

internal sealed class
    TimeoutUncachedAlternativeRecommendationGenerator
    : IUncachedAlternativeRecommendationGenerator
{
    private readonly
        SemanticKernelAlternativeRecommendationGenerator
        _innerGenerator;

    private readonly AlternativeRecommendationAiOptions
        _options;

    public TimeoutUncachedAlternativeRecommendationGenerator(
        SemanticKernelAlternativeRecommendationGenerator innerGenerator,
        AlternativeRecommendationAiOptions options)
    {
        ArgumentNullException.ThrowIfNull(
            innerGenerator);

        ArgumentNullException.ThrowIfNull(
            options);

        _innerGenerator =
            innerGenerator;

        _options =
            options;
    }

    public async Task<AlternativeRecommendationResult>
        GenerateAsync(
            AlternativeRecommendationRequest request,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            request);

        cancellationToken.ThrowIfCancellationRequested();

        using var timeoutCancellation =
            new CancellationTokenSource(
                _options.RequestTimeout);

        using var linkedCancellation =
            CancellationTokenSource
                .CreateLinkedTokenSource(
                    cancellationToken,
                    timeoutCancellation.Token);

        try
        {
            return await _innerGenerator.GenerateAsync(
                request,
                linkedCancellation.Token);
        }
        catch (OperationCanceledException exception)
            when (
                !cancellationToken.IsCancellationRequested
                && timeoutCancellation.IsCancellationRequested)
        {
            throw new TimeoutException(
                $"LLM request exceeded the configured " +
                $"timeout of {_options.RequestTimeout}.",
                exception);
        }
    }
}