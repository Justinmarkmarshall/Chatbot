using Chatbot.Models;
using Chatbot.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Chatbot.Controllers;

[ApiController]
[Authorize]
[Route("api/chat/sessions/{sessionId:guid}/documents")]
public sealed class DocumentsController(DocumentService documents) : ControllerBase
{
    [HttpGet]
    public Task<IActionResult> List(Guid sessionId, CancellationToken ct) => Execute(async () => Ok(await documents.ListAsync(User, sessionId, ct)));

    [HttpGet("{id:guid}")]
    public Task<IActionResult> Status(Guid sessionId, Guid id, CancellationToken ct) => Execute(async () => Ok(await documents.GetAsync(User, sessionId, id, ct)));

    [HttpGet("{id:guid}/original")]
    public Task<IActionResult> Download(Guid sessionId, Guid id, CancellationToken ct) => Execute(async () =>
    {
        var original = await documents.DownloadAsync(User, sessionId, id, ct);
        Response.Headers.XContentTypeOptions = "nosniff";
        return File(original.Content, "application/octet-stream", original.Document.FileName);
    });

    [HttpDelete("{id:guid}")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Delete(Guid sessionId, Guid id, CancellationToken ct) => Execute(async () =>
    {
        await documents.DeleteAsync(User, sessionId, id, ct);
        return NoContent();
    });

    [HttpPost("{id:guid}/retry")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Retry(Guid sessionId, Guid id, CancellationToken ct) => Execute(async () =>
    {
        await documents.RetryAsync(User, sessionId, id, ct);
        return Accepted();
    });

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(DocumentService.MaxBytes + 65536)]
    [RequestFormLimits(MultipartBodyLengthLimit = DocumentService.MaxBytes + 65536)]
    public Task<IActionResult> Upload(Guid sessionId, IFormFile file, CancellationToken ct) => Execute(async () =>
    {
        if (file.Length > DocumentService.MaxBytes) return StatusCode(413, "The maximum file size is 10 MiB.");
        await using var stream = file.OpenReadStream();
        var document = await documents.UploadAsync(User, sessionId, file.FileName, stream, ct);
        return Created($"/api/chat/sessions/{sessionId}/documents/{document.Id}", document);
    });

    private async Task<IActionResult> Execute(Func<Task<IActionResult>> action)
    {
        Response.Headers.CacheControl = "no-store";
        try { return await action(); }
        catch (ChatNotFoundException) { return NotFound("Document or chat not found."); }
        catch (UnauthorizedAccessException) { return StatusCode(403); }
        catch (DocumentLimitException ex) { return Conflict(ex.Message); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
    }
}

