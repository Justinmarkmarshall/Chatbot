using Npgsql;

namespace Chatbot.Persistence;

public static class DatabaseConfiguration
{
    public static string ConnectionString(IConfiguration configuration)
    {
        // Preserve existing local user secrets and full connection-string Secret deployments.
        if (configuration.GetConnectionString("Chatbot") is { Length: > 0 } connection) return connection;
        string Required(string name) => configuration["Database:" + name]
            ?? throw new InvalidOperationException($"Configure Database:{name} or ConnectionStrings:Chatbot.");
        return new NpgsqlConnectionStringBuilder
        {
            Host = Required("Host"), Port = configuration.GetValue("Database:Port", 5432),
            Database = Required("Name"), Username = Required("Username"), Password = Required("Password")
        }.ConnectionString;
    }
}
