using Chatbot.Processing;

namespace Chatbot.Services;

public interface IQueryEmbedding
{
    Task<float[]> EmbedAsync(string question, CancellationToken ct);
}

// One lazy model per web process. Chats without ready documents do not load ONNX.
public sealed class QueryEmbedding(IConfiguration configuration) : IQueryEmbedding, IDisposable
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private OnnxEmbedder? model;

    public async Task<float[]> EmbedAsync(string question, CancellationToken ct)
    {
        await gate.WaitAsync(ct);
        try
        {
            // Keep synchronous tokenization/inference off the Blazor circuit's synchronization context.
            return await Task.Run(() =>
            {
                ct.ThrowIfCancellationRequested();
                model ??= DocumentWorker.LoadModel(configuration["Documents:ModelRoot"]
                    ?? throw new InvalidOperationException("Configure Documents:ModelRoot for document chat."));
                // Keep the existing 256-token model contract. Never silently truncate questions.
                if (model.Tokenize(question).Length > 256)
                    throw new ArgumentException("For document questions, use at most 254 MiniLM content tokens. Please shorten the question.");
                var vector = model.Embed(question);
                ct.ThrowIfCancellationRequested();
                return vector;
            }, ct);
        }
        finally { gate.Release(); }
    }

    public void Dispose() { model?.Dispose(); gate.Dispose(); }
}
