using System.Net;
using System.Text.Json;
using AIPlatform.Api.Options;
using AIPlatform.Api.Services;
using Chatbot.Models;
using Microsoft.Extensions.Options;

namespace Chatbot.UnitTests;

public class OllamaClientTests
{
    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) => send(request, ct);
    }
    private static async Task<string> Read(string payload, HttpStatusCode status = HttpStatusCode.OK, CancellationToken ct = default)
    {
        using var http = new HttpClient(new Handler((_, token) => { token.ThrowIfCancellationRequested(); return Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(payload) }); })) { BaseAddress = new("http://test.invalid") };
        var client = new OllamaClient(http, Options.Create(new OllamaOptions { BaseUrl = "http://test.invalid", Model = "test-model" }));
        var pieces = new List<string>();
        await foreach (var piece in client.StreamChatAsync([new("user", "question")], ct)) pieces.Add(piece);
        return string.Concat(pieces);
    }

    [Fact]
    public async Task FragmentsAndFinalContentAreReturnedAndReadingStopsAtDone() =>
        Assert.Equal("Hello world", await Read("{\"message\":{\"content\":\"Hello\"}}\n{\"message\":{\"content\":\" world\"},\"done\":true}\ninvalid trailing data"));

    [Theory]
    [InlineData("{\"done\":true}")]
    [InlineData("{\"message\":null,\"done\":true}")]
    [InlineData("{}\n{\"done\":true}")]
    public async Task EmptyOrMissingMessageIsNotContent(string payload) => Assert.Equal("", await Read(payload));

    [Theory]
    [InlineData("")]
    [InlineData("{\"message\":{\"content\":\"partial\"}}")]
    public async Task EofWithoutDoneFails(string payload) => await Assert.ThrowsAsync<IOException>(() => Read(payload));

    [Theory]
    [InlineData("not json")]
    [InlineData("\n{\"done\":true}")]
    public async Task MalformedOrBlankProtocolLinesFail(string payload) => await Assert.ThrowsAsync<JsonException>(() => Read(payload));

    [Fact]
    public async Task ServerAndHttpErrorsFail()
    {
        var error = await Assert.ThrowsAsync<HttpRequestException>(() => Read("{\"error\":\"private server details\"}"));
        Assert.DoesNotContain("private server details", error.Message);
        await Assert.ThrowsAsync<HttpRequestException>(() => Read("error", HttpStatusCode.ServiceUnavailable));
    }

    [Fact]
    public async Task CancellationIsForwarded() => await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Read("", ct: new CancellationToken(true)));

    [Fact]
    public async Task CancellationInterruptsAnInFlightRequest()
    {
        using var cts = new CancellationTokenSource();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var http = new HttpClient(new Handler(async (_, ct) =>
        {
            entered.SetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, ct);
            throw new InvalidOperationException("Cancelled request must not finish normally");
        })) { BaseAddress = new("http://test.invalid") };
        var client = new OllamaClient(http, Options.Create(new OllamaOptions { BaseUrl = "http://test.invalid", Model = "test" }));
        async Task Consume()
        {
            await foreach (var _ in client.StreamChatAsync([new("user", "question")], cts.Token)) { }
        }
        var consuming = Consume();
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => consuming.WaitAsync(TimeSpan.FromSeconds(5)));
    }

    [Fact]
    public async Task RequestPreservesRolesOrderAndConfiguredModel()
    {
        ConversationMessage[] messages = [new("system", "rules"), new("user", "first"), new("assistant", "answer"), new("user", "next")];
        using var http = new HttpClient(new Handler(async (request, ct) =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("/api/chat", request.RequestUri!.AbsolutePath);
            using var json = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(ct));
            Assert.Equal("qwen-test", json.RootElement.GetProperty("model").GetString());
            Assert.True(json.RootElement.GetProperty("stream").GetBoolean());
            Assert.Equal(messages.Select(m => m.Role), json.RootElement.GetProperty("messages").EnumerateArray().Select(m => m.GetProperty("role").GetString()));
            Assert.Equal(messages.Select(m => m.Content), json.RootElement.GetProperty("messages").EnumerateArray().Select(m => m.GetProperty("content").GetString()));
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"done\":true}") };
        })) { BaseAddress = new("http://test.invalid") };
        var client = new OllamaClient(http, Options.Create(new OllamaOptions { BaseUrl = "http://test.invalid", Model = "qwen-test" }));
        await foreach (var _ in client.StreamChatAsync(messages)) { }
    }
}

