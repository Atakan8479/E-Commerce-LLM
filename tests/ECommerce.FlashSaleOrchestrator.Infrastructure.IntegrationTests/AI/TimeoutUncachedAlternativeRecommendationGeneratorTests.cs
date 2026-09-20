using System.Runtime.CompilerServices;
using ECommerce.FlashSaleOrchestrator.Application
    .AlternativeCandidates;
using ECommerce.FlashSaleOrchestrator.Application
    .AlternativeRecommendations;
using ECommerce.FlashSaleOrchestrator.Infrastructure.AI;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace ECommerce.FlashSaleOrchestrator
    .Infrastructure.IntegrationTests.AI;

public sealed class
    TimeoutUncachedAlternativeRecommendationGeneratorTests
{
    [Fact]
    public async Task
        GenerateAsync_ShouldThrowTimeoutException_WhenLlmExceedsConfiguredTimeout()
    {
        var chatCompletionService =
            new BlockingChatCompletionService();

        var innerGenerator =
            new SemanticKernelAlternativeRecommendationGenerator(
                chatCompletionService);

        var generator =
            new TimeoutUncachedAlternativeRecommendationGenerator(
                innerGenerator,
                new AlternativeRecommendationAiOptions(
                    TimeSpan.FromMilliseconds(
                        50)));

        await Assert.ThrowsAsync<TimeoutException>(
            () =>
                generator.GenerateAsync(
                    CreateRequest()));

        Assert.Equal(
            1,
            chatCompletionService.CallCount);
    }

    [Fact]
    public async Task
        GenerateAsync_ShouldPropagateCallerCancellation_WithoutConvertingToTimeout()
    {
        var chatCompletionService =
            new BlockingChatCompletionService();

        var innerGenerator =
            new SemanticKernelAlternativeRecommendationGenerator(
                chatCompletionService);

        var generator =
            new TimeoutUncachedAlternativeRecommendationGenerator(
                innerGenerator,
                new AlternativeRecommendationAiOptions(
                    TimeSpan.FromSeconds(
                        5)));

        using var cancellationTokenSource =
            new CancellationTokenSource(
                TimeSpan.FromMilliseconds(
                    50));

        await Assert.ThrowsAnyAsync<
            OperationCanceledException>(
            () =>
                generator.GenerateAsync(
                    CreateRequest(),
                    cancellationTokenSource.Token));

        Assert.Equal(
            1,
            chatCompletionService.CallCount);
    }

    private static AlternativeRecommendationRequest
        CreateRequest()
    {
        return new AlternativeRecommendationRequest(
            $"timeout-test-{Guid.NewGuid():N}",
            new DepletedProductContext(
                Guid.NewGuid(),
                "Depleted Product",
                "Electronics"),
            [
                new AlternativeCandidate(
                    Guid.NewGuid(),
                    "Alternative Product",
                    "Electronics",
                    10)
            ]);
    }

    private sealed class BlockingChatCompletionService
        : IChatCompletionService
    {
        public IReadOnlyDictionary<string, object?>
            Attributes
        { get; } =
                new Dictionary<string, object?>();

        public int CallCount { get; private set; }

        public async Task<
            IReadOnlyList<ChatMessageContent>>
            GetChatMessageContentsAsync(
                ChatHistory chatHistory,
                PromptExecutionSettings? executionSettings = null,
                Kernel? kernel = null,
                CancellationToken cancellationToken = default)
        {
            CallCount++;

            await Task.Delay(
                Timeout.InfiniteTimeSpan,
                cancellationToken);

            return Array.Empty<
                ChatMessageContent>();
        }

        public async IAsyncEnumerable<
            StreamingChatMessageContent>
            GetStreamingChatMessageContentsAsync(
                ChatHistory chatHistory,
                PromptExecutionSettings? executionSettings = null,
                Kernel? kernel = null,
                [EnumeratorCancellation]
                CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;

            yield break;
        }
    }
}