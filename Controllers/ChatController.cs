using AIPlatform.Api.Models;
using AIPlatform.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace AIPlatform.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ChatController(IOllamaClient ollamaClient) : ControllerBase
{
    [HttpPost]
    public async Task Chat(
        ChatRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            await Response.WriteAsync("Message is required.", cancellationToken);
            return;
        }

        Response.ContentType = "text/plain; charset=utf-8";
        await foreach (var chunk in ollamaClient.StreamChatAsync(request.Message, cancellationToken))
        {
            await Response.WriteAsync(chunk, cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }
    }
}