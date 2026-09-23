using System.Text.Json.Serialization;

namespace AIPlatform.Api.Models;

public sealed class OllamaChatResponse
{
    [JsonPropertyName("done")]
    public bool Done { get; init; }

    [JsonPropertyName("error")]
    public string? Error { get; init; }

    [JsonPropertyName("message")]
    public OllamaMessage Message { get; init; } = new();
}

public sealed class OllamaMessage
{
    [JsonPropertyName("role")]
    public string Role { get; init; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; init; } = string.Empty;
}
