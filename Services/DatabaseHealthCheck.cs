using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;

namespace Chatbot.Services;

public static class HealthEndpoints
{
    public static void MapChatbotHealthEndpoints(this WebApplication app)
    {
        app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions { Predicate = _ => false }).AllowAnonymous();
        app.MapHealthChecks("/health/ready").AllowAnonymous();
    }
}

public sealed class DatabaseHealthCheck(NpgsqlDataSource source) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(2));
        try
        {
            await using var command = source.CreateCommand("SELECT 1");
            await command.ExecuteScalarAsync(timeout.Token);
            return HealthCheckResult.Healthy();
        }
        catch { return HealthCheckResult.Unhealthy("Database unavailable."); }
    }
}
