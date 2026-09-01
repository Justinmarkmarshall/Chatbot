using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using AIPlatform.Api.Models;
using AIPlatform.Api.Options;
using Microsoft.Extensions.Options;

namespace AIPlatform.Api.Services;

public sealed class OllamaClient(
    HttpClient httpClient,
    IOptions<OllamaOptions> options) : IOllamaClient
{
    private readonly OllamaOptions _options = options.Value;

    public async IAsyncEnumerable<string> StreamChatAsync(
        string message,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var request = new
        {
            model = _options.Model,
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = message
                }
            },
            stream = true
        };

        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, "/api/chat")
        {
            Content = JsonContent.Create(request)
        };
        using var response = await httpClient.SendAsync(
            requestMessage,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            var result = JsonSerializer.Deserialize<OllamaChatResponse>(line);
            if (!string.IsNullOrEmpty(result?.Message.Content))
            {
                yield return result.Message.Content;
            }
        }
    }
}