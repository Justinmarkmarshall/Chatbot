using System.Runtime.CompilerServices;
using System.Security.Claims;
using AIPlatform.Api.Services;
using Chatbot.Models;
using Chatbot.Persistence;
using Chatbot.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace Chatbot.UnitTests;

public class ChatServiceTests
{
    private static readonly ClaimsPrincipal User = new(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "stable-subject")], "test"));
    private readonly Store store = new();
    private readonly Retrieval retrieval = new();
    private readonly Generator generator = new();
    private readonly Guid session = Guid.NewGuid();
    private ChatService Service => new(store, generator, NullLogger<ChatService>.Instance, retrieval);
    private async Task<string> Ask(CancellationToken ct = default)
    {
        var pieces = new List<string>();
        await foreach (var piece in Service.SendAsync(User, session, " question ", ct)) pieces.Add(piece);
        return string.Concat(pieces);
    }

    [Fact]
    public async Task UploadsWithoutEvidenceDoNotGenerateAndFinishNormally()
    {
        retrieval.Evidence = new(true, []);
        Assert.Equal(ChatService.NoDocumentsResponse, await Ask());
        Assert.Equal(0, generator.Calls);
        Assert.Equal((ChatService.NoDocumentsResponse, "completed"), store.Replies.Single());
        Assert.True(store.Disposed);
    }

    [Fact]
    public async Task EmptyTabRetainsGeneralChatBehaviour()
    {
        retrieval.Evidence = new(false, []);
        Assert.Equal("answer", await Ask());
        Assert.Equal(store.History, generator.Messages);
        Assert.Equal(("answer", "completed"), store.Replies.Single());
    }

    [Fact]
    public async Task GroundedChatPassesOwnerTabQuestionAndSavesSourcesBeforeGeneration()
    {
        generator.Before = () => Assert.Equal(retrieval.Evidence.Sources, store.Sources);
        Assert.Equal("answer", await Ask());
        Assert.Equal(("stable-subject", session, "question"), retrieval.Request);
        Assert.Equal(("stable-subject", session, "question"), store.Request);
        Assert.Equal("system", generator.Messages![0].Role);
        Assert.Equal("question", generator.Messages[^1].Content);
        Assert.Contains(generator.Messages, m => m.Content.Contains("Evidence"));
        Assert.True(store.Disposed);
    }

    [Fact]
    public async Task RetrievalFailureNeverFallsBackAndFinalizesFailed()
    {
        retrieval.Error = new IOException("retrieval failed");
        await Assert.ThrowsAsync<IOException>(() => Ask());
        Assert.Equal(0, generator.Calls);
        Assert.Equal(("", "failed"), store.Replies.Single());
        Assert.True(store.Disposed);
    }

    [Fact]
    public async Task RetrievalCancellationNeverFallsBackAndFinalSaveUsesIndependentToken()
    {
        using var cts = new CancellationTokenSource();
        retrieval.Before = () => cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Ask(cts.Token));
        Assert.Equal(0, generator.Calls);
        Assert.Equal(("", "cancelled"), store.Replies.Single());
        Assert.True(store.Disposed);
    }

    [Fact]
    public async Task GenerationFailurePreservesPartialResponse()
    {
        generator.Error = new IOException("stream failed");
        await Assert.ThrowsAsync<IOException>(() => Ask());
        Assert.Equal(("answer", "failed"), store.Replies.Last());
        Assert.True(store.Disposed);
    }

    [Fact]
    public async Task CancellationAfterPartialReplyPreservesTextAndReleasesTurn()
    {
        using var cts = new CancellationTokenSource();
        generator.After = () => cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Ask(cts.Token));
        Assert.Equal(("answer", "cancelled"), store.Replies.Last());
        Assert.True(store.Disposed);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" \n")]
    public async Task EmptyGenerationFails(string response)
    {
        generator.Response = response;
        await Assert.ThrowsAsync<InvalidDataException>(() => Ask());
        Assert.Equal((response, "failed"), store.Replies.Last());
    }

    [Fact]
    public async Task LargePartialReplyIsCheckpointedBeforeCompletion()
    {
        generator.Response = new string('a', 1024);
        await Ask();
        Assert.Equal(new[] { "streaming", "completed" }, store.Replies.Select(r => r.Status));
        Assert.All(store.Replies, r => Assert.Equal(generator.Response, r.Content));
    }

    [Fact]
    public async Task SourcePersistenceFailurePreventsGeneration()
    {
        store.SourceError = new IOException("save failed");
        await Assert.ThrowsAsync<IOException>(() => Ask());
        Assert.Equal(0, generator.Calls);
        Assert.Equal("failed", store.Replies.Single().Status);
        Assert.True(store.Disposed);
    }

    private sealed class Retrieval : IDocumentRetrieval
    {
        public DocumentEvidence Evidence = new(true, [DocumentPromptTests.Source()]);
        public Exception? Error;
        public Action? Before;
        public (string, Guid, string) Request;
        public Task<DocumentEvidence> RetrieveAsync(string owner, Guid id, string question, CancellationToken ct)
        {
            Request = (owner, id, question);
            Before?.Invoke();
            ct.ThrowIfCancellationRequested();
            if (Error is not null) throw Error;
            return Task.FromResult(Evidence);
        }
    }

    private sealed class Generator : IOllamaClient
    {
        public int Calls;
        public string Response = "answer";
        public Exception? Error;
        public Action? Before, After;
        public IReadOnlyList<ConversationMessage>? Messages;
        public async IAsyncEnumerable<string> StreamChatAsync(IReadOnlyList<ConversationMessage> messages, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            Calls++; Messages = messages; Before?.Invoke();
            await Task.CompletedTask;
            yield return Response;
            After?.Invoke();
            cancellationToken.ThrowIfCancellationRequested();
            if (Error is not null) throw Error;
        }
    }

    private sealed class Store : IChatStore, IAsyncDisposable
    {
        public IReadOnlyList<ConversationMessage> History = [new("user", "earlier"), new("assistant", "old answer"), new("user", "question")];
        public List<(string Content, string Status)> Replies = [];
        public IReadOnlyList<DocumentSource>? Sources;
        public Exception? SourceError;
        public bool Disposed;
        public (string, Guid, string) Request;
        public Task<ChatTurn> BeginTurnAsync(string owner, Guid id, string prompt, CancellationToken ct)
        {
            Request = (owner, id, prompt);
            return Task.FromResult(new ChatTurn(this, owner, id, Guid.NewGuid(), Guid.NewGuid(), History));
        }
        public Task SaveSourcesAsync(ChatTurn turn, IReadOnlyList<DocumentSource> sources, CancellationToken ct)
        {
            if (SourceError is not null) throw SourceError;
            Sources = sources; return Task.CompletedTask;
        }
        public Task SaveReplyAsync(ChatTurn turn, string content, string status, CancellationToken ct)
        {
            Assert.False(ct.IsCancellationRequested);
            Replies.Add((content, status)); return Task.CompletedTask;
        }
        public ValueTask DisposeAsync() { Disposed = true; return ValueTask.CompletedTask; }
        public Task<IReadOnlyList<ChatSession>> ListAsync(string owner, CancellationToken ct) => throw new NotSupportedException();
        public Task<ChatSession> CreateAsync(string owner, string title, CancellationToken ct) => throw new NotSupportedException();
        public Task RenameAsync(string owner, Guid id, string title, CancellationToken ct) => throw new NotSupportedException();
        public Task DeleteAsync(string owner, Guid id, CancellationToken ct) => throw new NotSupportedException();
        public Task DeleteMessageAsync(string owner, Guid sessionId, Guid messageId, CancellationToken ct) => throw new NotSupportedException();
        public Task<ChatConversation> GetAsync(string owner, Guid id, CancellationToken ct) => throw new NotSupportedException();
    }
}
