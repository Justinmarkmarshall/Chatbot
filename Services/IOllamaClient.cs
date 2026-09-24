namespace AIPlatform.Api.Services;

public interface IOllamaClient
{
    IAsyncEnumerable<string> StreamChatAsync(
        IReadOnlyList<Chatbot.Models.ConversationMessage> messages,
        CancellationToken cancellationToken = default);
}
