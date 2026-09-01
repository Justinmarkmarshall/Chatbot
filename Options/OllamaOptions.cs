namespace AIPlatform.Api.Options;

public sealed class OllamaOptions
{
    public const string SectionName = "Ollama";

    public required string BaseUrl { get; init; }

    public required string Model { get; init; }
}