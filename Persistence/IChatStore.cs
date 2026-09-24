using Chatbot.Models;

namespace Chatbot.Persistence;

public interface IChatStore
{
    Task<IReadOnlyList<ChatSession>> ListAsync(string owner, CancellationToken ct);
    Task<ChatSession> CreateAsync(string owner, string title, CancellationToken ct);
    Task RenameAsync(string owner, Guid id, string title, CancellationToken ct);
    Task DeleteAsync(string owner, Guid id, CancellationToken ct);
    Task DeleteMessageAsync(string owner, Guid sessionId, Guid messageId, CancellationToken ct);
    Task<ChatConversation> GetAsync(string owner, Guid id, CancellationToken ct);
    Task<ChatTurn> BeginTurnAsync(string owner, Guid sessionId, string prompt, CancellationToken ct);
    Task SaveSourcesAsync(ChatTurn turn, IReadOnlyList<DocumentSource> sources, CancellationToken ct);
    Task SaveReplyAsync(ChatTurn turn, string content, string status, CancellationToken ct);
}
