using System.Security.Claims;
using Marshall.Authentication.Google;
using Microsoft.AspNetCore.Authentication;

namespace Chatbot.Services;

// Cookie-backed sessions: Google establishes identity; Data Protection protects the ticket.
public sealed class ChatbotGoogleSessionHandler : IGoogleSessionHandler
{
    public Task<AuthenticationTicket?> CreateTicketAsync(
        HttpContext context, ClaimsPrincipal externalPrincipal, string applicationScheme)
    {
        var subject = externalPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(subject))
            return Task.FromResult<AuthenticationTicket?>(null);

        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, subject) };
        foreach (var type in new[] { ClaimTypes.Name, ClaimTypes.Email })
        {
            var value = externalPrincipal.FindFirstValue(type);
            if (!string.IsNullOrWhiteSpace(value)) claims.Add(new Claim(type, value));
        }

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, applicationScheme));
        return Task.FromResult<AuthenticationTicket?>(new AuthenticationTicket(
            principal, new AuthenticationProperties { IsPersistent = false }, applicationScheme));
    }

    public Task<bool> ValidateAsync(HttpContext context, ClaimsPrincipal principal) =>
        Task.FromResult(principal.Identity?.IsAuthenticated == true &&
            !string.IsNullOrWhiteSpace(principal.FindFirstValue(ClaimTypes.NameIdentifier)));

    // There is no server-side session store; the package clears the browser cookies.
    public Task RevokeAsync(HttpContext context, ClaimsPrincipal principal) => Task.CompletedTask;
}
