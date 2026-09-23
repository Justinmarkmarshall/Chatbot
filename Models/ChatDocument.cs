namespace Chatbot.Models;

public sealed record ChatDocument(Guid Id, Guid SessionId, string FileName, string MediaType,
    string Status, long? ByteLength, string? Sha256, string? ErrorCode, DateTime CreatedAt, DateTime UpdatedAt, string ProcessingStatus = "not_started", string? ProcessingError = null, int ChunkCount = 0);
public sealed record OriginalDocument(ChatDocument Document, byte[] Content);
public sealed class DocumentLimitException(string message) : Exception(message);

