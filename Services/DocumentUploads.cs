namespace Chatbot.Services;

// One gate per process bounds in-memory originals while accepting concurrent uploads.
public sealed class DocumentUploads : IDisposable
{
    public SemaphoreSlim Slots { get; } = new(2, 2);
    public void Dispose() => Slots.Dispose();
}
