using Chatbot.Models;
using System.Text.Json;
using Npgsql;

namespace Chatbot.Persistence;

// Every query touching user data is scoped by the authenticated subject. Store callers
// receive that subject from ChatService, never from an API body or component parameter.
public sealed class ChatStore(NpgsqlDataSource source) : IChatStore
{
    public async Task<IReadOnlyList<ChatSession>> ListAsync(string owner, CancellationToken ct)
    {
        await using var cmd = source.CreateCommand("SELECT id,title,created_at,updated_at FROM chat_sessions WHERE owner_subject=$1 ORDER BY updated_at DESC,id");
        cmd.Parameters.AddWithValue(owner);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        var sessions = new List<ChatSession>();
        while (await reader.ReadAsync(ct)) sessions.Add(ReadSession(reader));
        return sessions;
    }

    public async Task<ChatSession> CreateAsync(string owner, string title, CancellationToken ct)
    {
        await using var cmd = source.CreateCommand("INSERT INTO chat_sessions(id,owner_subject,title) VALUES($1,$2,$3) RETURNING id,title,created_at,updated_at");
        cmd.Parameters.AddWithValue(Guid.NewGuid()); cmd.Parameters.AddWithValue(owner); cmd.Parameters.AddWithValue(title);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        await reader.ReadAsync(ct);
        return ReadSession(reader);
    }

    public async Task RenameAsync(string owner, Guid id, string title, CancellationToken ct)
    {
        await using var cmd = source.CreateCommand("UPDATE chat_sessions SET title=$3,updated_at=clock_timestamp() WHERE id=$1 AND owner_subject=$2");
        cmd.Parameters.AddWithValue(id); cmd.Parameters.AddWithValue(owner); cmd.Parameters.AddWithValue(title);
        if (await cmd.ExecuteNonQueryAsync(ct) != 1) throw new ChatNotFoundException();
    }

    public async Task DeleteAsync(string owner, Guid id, CancellationToken ct)
    {
        await using var gate = (await AcquireAsync(owner, id, ct))!;
        await using var cmd = source.CreateCommand("DELETE FROM chat_sessions WHERE id=$1 AND owner_subject=$2");
        cmd.Parameters.AddWithValue(id); cmd.Parameters.AddWithValue(owner);
        if (await cmd.ExecuteNonQueryAsync(ct) != 1) throw new ChatNotFoundException();
    }

    public async Task DeleteMessageAsync(string owner, Guid sessionId, Guid messageId, CancellationToken ct)
    {
        await using var gate = (await AcquireAsync(owner, sessionId, ct))!;
        await using var cmd = source.CreateCommand("""
            DELETE FROM chat_messages m USING chat_sessions s
            WHERE m.id=$1 AND m.session_id=s.id AND s.id=$2 AND s.owner_subject=$3
            """);
        cmd.Parameters.AddWithValue(messageId); cmd.Parameters.AddWithValue(sessionId); cmd.Parameters.AddWithValue(owner);
        if (await cmd.ExecuteNonQueryAsync(ct) != 1) throw new ChatNotFoundException();
    }

    public async Task<ChatConversation> GetAsync(string owner, Guid id, CancellationToken ct)
    {
        // A crashed process releases its transaction lock. Only an idle session can be
        // repaired; loading history must never mark another replica's active reply failed.
        await using (var gate = await AcquireAsync(owner, id, ct, allowBusy: true))
            if (gate is not null) await RepairAsync(owner, id, ct);

        await using var connection = await source.OpenConnectionAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead, ct);
        await using var cmd = new NpgsqlCommand("SELECT id,title,created_at,updated_at FROM chat_sessions WHERE id=$1 AND owner_subject=$2", connection, transaction);
        cmd.Parameters.AddWithValue(id); cmd.Parameters.AddWithValue(owner);
        ChatSession session;
        await using (var reader = await cmd.ExecuteReaderAsync(ct))
        {
            if (!await reader.ReadAsync(ct)) throw new ChatNotFoundException();
            session = ReadSession(reader);
        }
        await using var messages = new NpgsqlCommand("""
            SELECT m.id,m.turn_id,m.sequence,m.role,m.content,m.status,m.created_at,m.document_sources
            FROM chat_messages m JOIN chat_sessions s ON s.id=m.session_id
            WHERE s.id=$1 AND s.owner_subject=$2 ORDER BY m.sequence
            """, connection, transaction);
        messages.Parameters.AddWithValue(id); messages.Parameters.AddWithValue(owner);
        var result = new List<StoredChatMessage>();
        await using (var reader = await messages.ExecuteReaderAsync(ct))
            while (await reader.ReadAsync(ct)) result.Add(new(reader.GetGuid(0), reader.GetGuid(1), reader.GetInt64(2), reader.GetString(3), reader.GetString(4), reader.GetString(5), reader.GetDateTime(6),
                reader.IsDBNull(7) ? null : JsonSerializer.Deserialize<DocumentSource[]>(reader.GetString(7))));
        await transaction.CommitAsync(ct);
        return new(session, result);
    }

    public async Task<ChatTurn> BeginTurnAsync(string owner, Guid sessionId, string prompt, CancellationToken ct)
    {
        var gate = (await AcquireAsync(owner, sessionId, ct))!;
        try
        {
            await RepairAsync(owner, sessionId, ct);
            await using var connection = await source.OpenConnectionAsync(ct);
            await using var transaction = await connection.BeginTransactionAsync(ct);
            // Ten complete turns, in original order. Failed/partial turns remain visible
            // in history but are not passed back to the model as successful conversation.
            await using var historyCommand = new NpgsqlCommand("""
                SELECT role,content FROM (
                    SELECT m.role,m.content,m.sequence FROM chat_messages m JOIN chat_sessions s ON s.id=m.session_id
                    WHERE s.id=$1 AND s.owner_subject=$2 AND m.status='completed'
                    AND EXISTS (SELECT 1 FROM chat_messages a WHERE a.session_id=m.session_id
                        AND a.turn_id=m.turn_id AND a.role='assistant' AND a.status='completed')
                    AND EXISTS (SELECT 1 FROM chat_messages u WHERE u.session_id=m.session_id
                        AND u.turn_id=m.turn_id AND u.role='user' AND u.status='completed')
                    ORDER BY m.sequence DESC LIMIT 20
                ) history ORDER BY sequence
                """, connection, transaction);
            historyCommand.Parameters.AddWithValue(sessionId); historyCommand.Parameters.AddWithValue(owner);
            var history = new List<ConversationMessage>();
            await using (var reader = await historyCommand.ExecuteReaderAsync(ct))
                while (await reader.ReadAsync(ct)) history.Add(new(reader.GetString(0), reader.GetString(1)));
            Guid turnId = Guid.NewGuid(), assistantId = Guid.NewGuid();
            foreach (string role in new[] { "user", "assistant" })
            {
                await using var insert = new NpgsqlCommand("""
                    INSERT INTO chat_messages(id,session_id,turn_id,role,content,status)
                    SELECT $1,id,$3,$4,$5,$6 FROM chat_sessions WHERE id=$2 AND owner_subject=$7
                    """, connection, transaction);
                insert.Parameters.AddWithValue(role == "user" ? Guid.NewGuid() : assistantId);
                insert.Parameters.AddWithValue(sessionId); insert.Parameters.AddWithValue(turnId); insert.Parameters.AddWithValue(role);
                insert.Parameters.AddWithValue(role == "user" ? prompt : ""); insert.Parameters.AddWithValue(role == "user" ? "completed" : "streaming"); insert.Parameters.AddWithValue(owner);
                if (await insert.ExecuteNonQueryAsync(ct) != 1) throw new ChatNotFoundException();
            }
            await using var touch = new NpgsqlCommand("UPDATE chat_sessions SET updated_at=clock_timestamp(),active_turn_id=$3 WHERE id=$1 AND owner_subject=$2", connection, transaction);
            touch.Parameters.AddWithValue(sessionId); touch.Parameters.AddWithValue(owner);
            touch.Parameters.AddWithValue(turnId);
            await touch.ExecuteNonQueryAsync(ct);
            await transaction.CommitAsync(ct);
            history.Add(new("user", prompt));
            return new(gate, owner, sessionId, turnId, assistantId, history);
        }
        catch { await gate.DisposeAsync(); throw; }
    }

    public async Task SaveSourcesAsync(ChatTurn turn, IReadOnlyList<DocumentSource> sources, CancellationToken ct)
    {
        await using var cmd = source.CreateCommand("""
            UPDATE chat_messages m SET document_sources=$4::jsonb FROM chat_sessions s
            WHERE m.session_id=s.id AND s.id=$1 AND s.owner_subject=$2 AND s.active_turn_id=$3
              AND m.id=$5 AND m.role='assistant' AND m.status='streaming'
            """);
        cmd.Parameters.AddWithValue(turn.SessionId); cmd.Parameters.AddWithValue(turn.Owner);
        cmd.Parameters.AddWithValue(turn.TurnId); cmd.Parameters.AddWithValue(JsonSerializer.Serialize(sources));
        cmd.Parameters.AddWithValue(turn.AssistantId);
        if (await cmd.ExecuteNonQueryAsync(ct) != 1) throw new ChatNotFoundException();
    }

    public async Task SaveReplyAsync(ChatTurn turn, string content, string status, CancellationToken ct)
    {
        await using var connection = await source.OpenConnectionAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);
        await using var cmd = new NpgsqlCommand("""
            UPDATE chat_messages m SET content=$3,status=$4 FROM chat_sessions s
            WHERE m.session_id=s.id AND s.id=$1 AND s.owner_subject=$2 AND s.active_turn_id=$6 AND m.id=$5 AND m.role='assistant'
            """, connection, transaction);
        cmd.Parameters.AddWithValue(turn.SessionId); cmd.Parameters.AddWithValue(turn.Owner);
        cmd.Parameters.AddWithValue(content); cmd.Parameters.AddWithValue(status); cmd.Parameters.AddWithValue(turn.AssistantId);
        cmd.Parameters.AddWithValue(turn.TurnId);
        if (await cmd.ExecuteNonQueryAsync(ct) != 1) throw new ChatNotFoundException();
        await using var touch = new NpgsqlCommand("UPDATE chat_sessions SET updated_at=clock_timestamp() WHERE id=$1 AND owner_subject=$2 AND active_turn_id=$3", connection, transaction);
        touch.Parameters.AddWithValue(turn.SessionId); touch.Parameters.AddWithValue(turn.Owner); touch.Parameters.AddWithValue(turn.TurnId);
        if (await touch.ExecuteNonQueryAsync(ct) != 1) throw new ChatNotFoundException();
        await transaction.CommitAsync(ct);
    }

    private async Task RepairAsync(string owner, Guid id, CancellationToken ct)
    {
        await using var connection = await source.OpenConnectionAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);
        await using var repair = new NpgsqlCommand("""
            UPDATE chat_messages m SET status='interrupted' FROM chat_sessions s
            WHERE m.session_id=s.id AND s.id=$1 AND s.owner_subject=$2 AND m.status='streaming'
            """, connection, transaction);
        repair.Parameters.AddWithValue(id); repair.Parameters.AddWithValue(owner);
        await repair.ExecuteNonQueryAsync(ct);
        await using var clear = new NpgsqlCommand("UPDATE chat_sessions SET active_turn_id=NULL WHERE id=$1 AND owner_subject=$2", connection, transaction);
        clear.Parameters.AddWithValue(id); clear.Parameters.AddWithValue(owner);
        await clear.ExecuteNonQueryAsync(ct);
        await transaction.CommitAsync(ct);
    }

    private async Task<SessionGate?> AcquireAsync(string owner, Guid id, CancellationToken ct, bool allowBusy = false)
    {
        var connection = await source.OpenConnectionAsync(ct);
        try
        {
            var transaction = await connection.BeginTransactionAsync(ct);
            await using var cmd = new NpgsqlCommand("""
                SELECT pg_try_advisory_xact_lock(hashtextextended(id::text, 0))
                FROM chat_sessions WHERE id=$1 AND owner_subject=$2
                """, connection, transaction);
            cmd.Parameters.AddWithValue(id); cmd.Parameters.AddWithValue(owner);
            var acquired = await cmd.ExecuteScalarAsync(ct);
            if (acquired is null) throw new ChatNotFoundException();
            if (!(bool)acquired)
            {
                await transaction.DisposeAsync();
                await connection.DisposeAsync();
                if (allowBusy) return null;
                throw new ChatBusyException();
            }
            return new(connection, transaction);
        }
        catch { await connection.DisposeAsync(); throw; }
    }

    private static ChatSession ReadSession(NpgsqlDataReader r) => new(r.GetGuid(0), r.GetString(1), r.GetDateTime(2), r.GetDateTime(3));
}

// An otherwise idle, dedicated transaction owns the cross-process generation lock.
// Message commits use a separate pooled connection so they survive a process crash.
public sealed class SessionGate(NpgsqlConnection connection, NpgsqlTransaction transaction) : IAsyncDisposable
{
    public async ValueTask DisposeAsync()
    {
        try { await transaction.DisposeAsync(); }
        finally { await connection.DisposeAsync(); }
    }
}

public sealed record ChatTurn(IAsyncDisposable Gate, string Owner, Guid SessionId, Guid TurnId, Guid AssistantId,
    IReadOnlyList<ConversationMessage> History) : IAsyncDisposable
{
    public ValueTask DisposeAsync() => Gate.DisposeAsync();
}
