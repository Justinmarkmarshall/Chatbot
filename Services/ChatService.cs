using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Text;
using AIPlatform.Api.Services;
using Chatbot.Models;
using Chatbot.Persistence;

namespace Chatbot.Services;

public sealed class ChatService(IChatStore store, IOllamaClient ollama, ILogger<ChatService> logger, IDocumentRetrieval retrieval)
{
    public const string NoDocumentsResponse = "I don't have any processed documents in this chat to answer from yet. Upload a document or check its processing status.";
    public const int MaximumTitleLength = 120;
    public const int MaximumMessageLength = 16000;
    public Task<IReadOnlyList<ChatSession>> ListAsync(ClaimsPrincipal user, CancellationToken ct = default) => store.ListAsync(Owner(user), ct);
    public Task<ChatConversation> GetAsync(ClaimsPrincipal user, Guid id, CancellationToken ct = default) => store.GetAsync(Owner(user), id, ct);
    public Task<ChatSession> CreateAsync(ClaimsPrincipal user, string title = "New chat", CancellationToken ct = default) => store.CreateAsync(Owner(user), Title(title), ct);
    public Task RenameAsync(ClaimsPrincipal user, Guid id, string title, CancellationToken ct = default) => store.RenameAsync(Owner(user), id, Title(title), ct);
    public Task DeleteAsync(ClaimsPrincipal user, Guid id, CancellationToken ct = default) => store.DeleteAsync(Owner(user), id, ct);
    public Task DeleteMessageAsync(ClaimsPrincipal user, Guid sessionId, Guid messageId, CancellationToken ct = default) => store.DeleteMessageAsync(Owner(user), sessionId, messageId, ct);

    public async IAsyncEnumerable<string> SendAsync(ClaimsPrincipal user, Guid sessionId, string message,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var total = Stopwatch.StartNew();
        var generation = new Stopwatch();
        string owner = Owner(user);
        if (string.IsNullOrWhiteSpace(message) || message.Length > MaximumMessageLength)
            throw new ArgumentException($"Message must contain 1 to {MaximumMessageLength} characters.");
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromMinutes(5));
        await using var turn = await store.BeginTurnAsync(owner, sessionId, message.Trim(), deadline.Token);
        var reply = new StringBuilder();
        bool completed = false;
        var checkpoint = Stopwatch.StartNew();
        int savedLength = 0;
        try
        {
            var evidence = await retrieval.RetrieveAsync(owner, sessionId, message.Trim(), deadline.Token);
            await store.SaveSourcesAsync(turn, evidence.Sources, deadline.Token);
            if (evidence.Sources.Count == 0 && evidence.HasUploads)
            {
                reply.Append(NoDocumentsResponse);
                yield return NoDocumentsResponse;
                deadline.Token.ThrowIfCancellationRequested();
                completed = true;
                yield break;
            }
            var modelMessages = evidence.HasUploads
                ? DocumentPrompt.Build(turn.History, evidence.Sources)
                : turn.History;
            generation.Start();
            await foreach (var chunk in ollama.StreamChatAsync(modelMessages, deadline.Token))
            {
                reply.Append(chunk);
                if (checkpoint.Elapsed >= TimeSpan.FromSeconds(1) || reply.Length - savedLength >= 1024)
                {
                    await store.SaveReplyAsync(turn, reply.ToString(), "streaming", deadline.Token);
                    savedLength = reply.Length;
                    checkpoint.Restart();
                }
                yield return chunk;
            }
            deadline.Token.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(reply.ToString())) throw new InvalidDataException("Ollama returned an empty response.");
            completed = true;
        }
        finally
        {
            generation.Stop();
            // Request cancellation must not cancel the final persistence operation.
            using var saveTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            string status = completed ? "completed" : cancellationToken.IsCancellationRequested ? "cancelled" : "failed";
            try { await store.SaveReplyAsync(turn, reply.ToString(), status, saveTimeout.Token); }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not finalize reply for chat {SessionId}. It will be marked interrupted when next opened.", sessionId);
                throw;
            }
            finally
            {
                logger.LogInformation("Chat {ChatId}: generation {GenerationMs} ms, total {TotalMs} ms, completed {Completed}", sessionId, generation.Elapsed.TotalMilliseconds, total.Elapsed.TotalMilliseconds, completed);
            }
        }
    }

    private static string Owner(ClaimsPrincipal principal)
    {
        string? subject = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (principal.Identity?.IsAuthenticated != true || string.IsNullOrWhiteSpace(subject) || subject.Length > 255)
            throw new UnauthorizedAccessException("An authenticated Google subject is required.");
        return subject;
    }

    private static string Title(string? title)
    {
        title = title?.Trim();
        if (string.IsNullOrWhiteSpace(title) || title.Length > MaximumTitleLength)
            throw new ArgumentException($"Title must contain 1 to {MaximumTitleLength} characters.");
        return title;
    }
}
