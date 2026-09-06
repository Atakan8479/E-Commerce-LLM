using System.Runtime.CompilerServices;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace ECommerce.FlashSaleOrchestrator.Infrastructure.IntegrationTests.AI;

internal sealed class FakeChatCompletionService
    : IChatCompletionService
{
    private readonly Func<int, string>
        _responseFactory;

    public FakeChatCompletionService(
        string response)
        : this(
            _ => response)
    {
    }

    public FakeChatCompletionService(
        Func<int, string> responseFactory)
    {
        ArgumentNullException.ThrowIfNull(
            responseFactory);

        _responseFactory =
            responseFactory;
    }

    public IReadOnlyDictionary<string, object?> Attributes { get; } =
        new Dictionary<string, object?>();

    public int CallCount { get; private set; }

    public CancellationToken? ReceivedCancellationToken { get; private set; }

    public Task<IReadOnlyList<ChatMessageContent>> GetChatMessageContentsAsync(
        ChatHistory chatHistory,
        PromptExecutionSettings? executionSettings = null,
        Kernel? kernel = null,
        CancellationToken cancellationToken = default)
    {
        CallCount++;

        ReceivedCancellationToken =
            cancellationToken;

        cancellationToken.ThrowIfCancellationRequested();

        var responseContent =
            _responseFactory(
                CallCount);

        IReadOnlyList<ChatMessageContent> response =
        [
            new ChatMessageContent(
                AuthorRole.Assistant,
                responseContent)
        ];

        return Task.FromResult(
            response);
    }

    public async IAsyncEnumerable<StreamingChatMessageContent>
        GetStreamingChatMessageContentsAsync(
            ChatHistory chatHistory,
            PromptExecutionSettings? executionSettings = null,
            Kernel? kernel = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;

        yield break;
    }
}