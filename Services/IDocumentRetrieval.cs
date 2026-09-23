using Chatbot.Models;
namespace Chatbot.Services;
public interface IDocumentRetrieval
{
    Task<DocumentEvidence> RetrieveAsync(string owner, Guid sessionId, string question, CancellationToken ct);
}
