using AIPlatform.Api.Models;
using Chatbot.Models;
using Chatbot.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIPlatform.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/chat")]
public sealed class ChatController(ChatService chats, ILogger<ChatController> logger) : ControllerBase
{
    [HttpGet("antiforgery")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public IActionResult Antiforgery([FromServices] IAntiforgery antiforgery) =>
        Ok(new { token = antiforgery.GetAndStoreTokens(HttpContext).RequestToken });

    [HttpGet("sessions")]
    public async Task<IActionResult> List(CancellationToken ct) => await Execute(async () => Ok(await chats.ListAsync(User, ct)));

    [HttpPost("sessions")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateChatRequest request, CancellationToken ct) => await Execute(async () =>
    {
        var session = await chats.CreateAsync(User, request.Title ?? "New chat", ct);
        return Created($"/api/chat/sessions/{session.Id}", session);
    });

    [HttpGet("sessions/{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct) => await Execute(async () => Ok(await chats.GetAsync(User, id, ct)));

    [HttpPatch("sessions/{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Rename(Guid id, RenameChatRequest request, CancellationToken ct) => await Execute(async () =>
    {
        await chats.RenameAsync(User, id, request.Title, ct);
        return NoContent();
    });

    [HttpDelete("sessions/{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct) => await Execute(async () =>
    {
        await chats.DeleteAsync(User, id, ct);
        return NoContent();
    });

    [HttpDelete("sessions/{sessionId:guid}/messages/{messageId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteMessage(Guid sessionId, Guid messageId, CancellationToken ct) => await Execute(async () =>
    {
        await chats.DeleteMessageAsync(User, sessionId, messageId, ct);
        return NoContent();
    });

    [HttpPost("sessions/{id:guid}/messages")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Send(Guid id, ChatRequest request, CancellationToken ct) => Stream(id, request.Message, ct);

    // Original streaming URL remains available. Reuse X-Chat-Session-Id on later sends.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Chat(ChatRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Message) || request.Message.Length > ChatService.MaximumMessageLength)
            return BadRequest("A message of 1 to 16000 characters is required.");
        return await Execute(async () =>
        {
            Guid id = request.SessionId ?? (await chats.CreateAsync(User, ct: ct)).Id;
            return await Stream(id, request.Message, ct);
        });
    }

    private async Task<IActionResult> Stream(Guid id, string message, CancellationToken ct)
    {
        try
        {
            await using var stream = chats.SendAsync(User, id, message, ct).GetAsyncEnumerator(ct);
            // Ownership, concurrency and input failures precede HTTP 200.
            bool hasChunk = await stream.MoveNextAsync();
            Response.ContentType = "text/plain; charset=utf-8";
            Response.Headers.CacheControl = "no-store";
            Response.Headers["X-Chat-Session-Id"] = id.ToString();
            while (hasChunk)
            {
                await Response.WriteAsync(stream.Current, ct);
                await Response.Body.FlushAsync(ct);
                hasChunk = await stream.MoveNextAsync();
            }
            return new EmptyResult();
        }
        catch (Exception ex) when (!Response.HasStarted && ex is ChatNotFoundException or ChatBusyException or ArgumentException or UnauthorizedAccessException)
        { return Failure(ex); }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { return new EmptyResult(); }
        catch (Exception ex)
        {
            logger.LogError(ex, "Chat reply failed for session {SessionId}", id);
            if (Response.HasStarted) { HttpContext.Abort(); return new EmptyResult(); }
            return StatusCode(503, "The reply could not be completed. Reload the chat to see its saved status.");
        }
    }

    private async Task<IActionResult> Execute(Func<Task<IActionResult>> action)
    {
        Response.Headers.CacheControl = "no-store";
        try { return await action(); }
        catch (Exception ex) when (ex is ChatNotFoundException or ChatBusyException or ArgumentException or UnauthorizedAccessException) { return Failure(ex); }
    }

    private IActionResult Failure(Exception ex) => ex switch
    {
        ChatNotFoundException => NotFound("Chat not found."),
        ChatBusyException => Conflict(ex.Message),
        UnauthorizedAccessException => StatusCode(403),
        _ => BadRequest(ex.Message)
    };
}

public sealed record CreateChatRequest(string? Title);
public sealed record RenameChatRequest(string Title);
