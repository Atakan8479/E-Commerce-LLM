using System.Runtime.CompilerServices;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace ECommerce.FlashSaleOrchestrator.Infrastructure.IntegrationTests.AI;

internal sealed class FakeChatCompletionService : IChatCompletionService
{
    private readonly string _response;

    public FakeChatCompletionService(string response)
    {
        _response = response;
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
        ReceivedCancellationToken = cancellationToken;

        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyList<ChatMessageContent> response =
        [
            new ChatMessageContent(
                AuthorRole.Assistant,
                _response)
        ];

        return Task.FromResult(response);
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