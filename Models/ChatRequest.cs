namespace AIPlatform.Api.Models;

public sealed record ChatRequest(string Message, Guid? SessionId = null);
