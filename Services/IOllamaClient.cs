namespace AIPlatform.Api.Services;

public interface IOllamaClient
{
    IAsyncEnumerable<string> StreamChatAsync(
        string message,
        CancellationToken cancellationToken = default);
}