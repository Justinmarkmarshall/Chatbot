namespace Chatbot.Models;

// A snapshot of the evidence supplied for one reply, not a claim that every chunk was cited.
public sealed record DocumentSource(int Number, Guid DocumentId, string FileName, string Heading,
    int ChunkIndex, int? PageNumber, string SourceText, double CosineDistance)
{
    public double CosineSimilarity => 1 - CosineDistance;
}

public sealed record DocumentEvidence(bool HasUploads, IReadOnlyList<DocumentSource> Sources);
