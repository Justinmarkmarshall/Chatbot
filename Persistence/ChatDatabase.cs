using Npgsql;

namespace Chatbot.Persistence;

public static class ChatDatabase
{
    public static async Task InitializeAsync(NpgsqlDataSource source, CancellationToken cancellationToken = default)
    {
        await using var connection = await source.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        // Serialize schema upgrades across app replicas. No destructive startup reset.
        await using var setup = new NpgsqlCommand("""
            SELECT pg_advisory_xact_lock(734918260114);
            CREATE TABLE IF NOT EXISTS chatbot_schema_migrations (
                version integer PRIMARY KEY, applied_at timestamptz NOT NULL DEFAULT now());
            SELECT COALESCE(MAX(version), 0) FROM chatbot_schema_migrations;
            """, connection, transaction);
        await setup.ExecuteNonQueryAsync(cancellationToken);
        await using var versionCommand = new NpgsqlCommand("SELECT COALESCE(MAX(version), 0) FROM chatbot_schema_migrations", connection, transaction);
        int version = (int)(await versionCommand.ExecuteScalarAsync(cancellationToken))!;
        string[] migrations = ["001_chat_sessions.sql", "002_documents.sql", "003_document_processing.sql", "004_reply_sources.sql"];
        if (version > migrations.Length) throw new InvalidOperationException("Database schema is newer than this Chatbot version.");
        for (int next = version + 1; next <= migrations.Length; next++)
        {
            await using var script = typeof(ChatDatabase).Assembly.GetManifestResourceStream("Chatbot.Persistence.Migrations." + migrations[next - 1])
                ?? throw new InvalidOperationException("Missing chat schema migration.");
            using var reader = new StreamReader(script);
            await using var migration = new NpgsqlCommand(await reader.ReadToEndAsync(cancellationToken), connection, transaction);
            await migration.ExecuteNonQueryAsync(cancellationToken);
            await using var record = new NpgsqlCommand("INSERT INTO chatbot_schema_migrations(version) VALUES ($1)", connection, transaction);
            record.Parameters.AddWithValue(next);
            await record.ExecuteNonQueryAsync(cancellationToken);
        }
        await transaction.CommitAsync(cancellationToken);
    }
}
