using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Chatbot.Models;
using Npgsql;

namespace Chatbot.Services;

public sealed class DocumentService(NpgsqlDataSource source, DocumentUploads uploads, ILogger<DocumentService> logger)
{
    public const int MaxBytes = 10 * 1024 * 1024;
    public const long OwnerQuotaBytes = 100L * 1024 * 1024;
    private const string Columns = "d.id,d.session_id,d.filename,d.media_type,d.status,d.byte_length,d.sha256,d.error_code,d.created_at,d.updated_at,d.processing_status,d.processing_error,d.chunk_count";

    public async Task<IReadOnlyList<ChatDocument>> ListAsync(ClaimsPrincipal user, Guid sessionId, CancellationToken ct = default)
    {
        string owner = Owner(user);
        await RequireSession(owner, sessionId, ct);
        await Expire(owner, ct);
        await using var cmd = source.CreateCommand($"SELECT {Columns} FROM chat_documents d JOIN chat_sessions s ON s.id=d.session_id WHERE s.id=$1 AND s.owner_subject=$2 ORDER BY d.created_at,d.id");
        cmd.Parameters.AddWithValue(sessionId); cmd.Parameters.AddWithValue(owner);
        var result = new List<ChatDocument>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct)) result.Add(Read(reader));
        return result;
    }

    public async Task<ChatDocument> GetAsync(ClaimsPrincipal user, Guid sessionId, Guid id, CancellationToken ct = default)
    {
        string owner = Owner(user);
        await Expire(owner, ct);
        await using var cmd = source.CreateCommand($"SELECT {Columns} FROM chat_documents d JOIN chat_sessions s ON s.id=d.session_id WHERE s.id=$1 AND s.owner_subject=$2 AND d.id=$3");
        cmd.Parameters.AddWithValue(sessionId); cmd.Parameters.AddWithValue(owner); cmd.Parameters.AddWithValue(id);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) throw new ChatNotFoundException();
        return Read(reader);
    }

    public async Task<OriginalDocument> DownloadAsync(ClaimsPrincipal user, Guid sessionId, Guid id, CancellationToken ct = default)
    {
        await using var cmd = source.CreateCommand($"SELECT {Columns},d.original FROM chat_documents d JOIN chat_sessions s ON s.id=d.session_id WHERE s.id=$1 AND s.owner_subject=$2 AND d.id=$3 AND d.status='uploaded'");
        cmd.Parameters.AddWithValue(sessionId); cmd.Parameters.AddWithValue(Owner(user)); cmd.Parameters.AddWithValue(id);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) throw new ChatNotFoundException();
        return new(Read(reader), reader.GetFieldValue<byte[]>(13));
    }

    public async Task<ChatDocument> UploadAsync(ClaimsPrincipal user, Guid sessionId, string fileName, Stream input, CancellationToken ct = default)
    {
        string owner = Owner(user);
        // Check ownership before reading even one byte of the upload.
        await RequireSession(owner, sessionId, ct);
        (fileName, string mediaType) = DocumentUploadValidation.FileName(fileName);
        if (!await uploads.Slots.WaitAsync(0, ct)) throw new DocumentLimitException("Two uploads are already running. Try again when one finishes.");
        Guid id = Guid.NewGuid();
        bool reserved = false;
        string failure = "upload_failed";
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(ct);
        deadline.CancelAfter(TimeSpan.FromMinutes(5));
        try
        {
            await Reserve(owner, sessionId, id, fileName, mediaType, deadline.Token);
            reserved = true;
            using var buffer = new MemoryStream();
            var block = new byte[64 * 1024];
            int count;
            while ((count = await input.ReadAsync(block, deadline.Token)) != 0)
            {
                if (DocumentUploadValidation.ExceedsSize(buffer.Length, count)) { failure = "size_limit"; throw new ArgumentException("The maximum file size is 10 MiB."); }
                await buffer.WriteAsync(block.AsMemory(0, count), deadline.Token);
            }
            var bytes = buffer.ToArray();
            failure = "invalid_content";
            DocumentUploadValidation.Content(bytes, mediaType);
            failure = "upload_failed";
            // Original, digest, length and Uploaded state publish atomically in one update.
            await using var complete = source.CreateCommand("""
                UPDATE chat_documents d SET original=$4,byte_length=$5,sha256=$6,status='uploaded',processing_status='queued',updated_at=clock_timestamp()
                FROM chat_sessions s WHERE d.session_id=s.id AND s.id=$1 AND s.owner_subject=$2 AND d.id=$3
                AND d.status='uploading' AND d.upload_deadline > clock_timestamp()
                """);
            complete.Parameters.AddWithValue(sessionId); complete.Parameters.AddWithValue(owner); complete.Parameters.AddWithValue(id);
            complete.Parameters.AddWithValue(bytes); complete.Parameters.AddWithValue((long)bytes.Length);
            complete.Parameters.AddWithValue(Convert.ToHexStringLower(SHA256.HashData(bytes)));
            if (await complete.ExecuteNonQueryAsync(deadline.Token) != 1) throw new OperationCanceledException("Upload expired before completion.");
            reserved = false;
            return await GetAsync(user, sessionId, id, ct);
        }
        finally
        {
            try
            {
                if (reserved)
                {
                    using var save = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                    await using var fail = source.CreateCommand("""
                        UPDATE chat_documents d SET status='failed',error_code=$4,updated_at=clock_timestamp()
                        FROM chat_sessions s WHERE d.session_id=s.id AND s.id=$1 AND s.owner_subject=$2 AND d.id=$3 AND d.status='uploading'
                        """);
                    fail.Parameters.AddWithValue(sessionId); fail.Parameters.AddWithValue(owner); fail.Parameters.AddWithValue(id);
                    fail.Parameters.AddWithValue(ct.IsCancellationRequested ? "upload_cancelled" : deadline.IsCancellationRequested ? "upload_interrupted" : failure);
                    try { await fail.ExecuteNonQueryAsync(save.Token); }
                    catch (Exception ex) { logger.LogError(ex, "Could not finalize document upload {DocumentId}; expiry recovery will mark it failed", id); }
                }
            }
            finally { uploads.Slots.Release(); }
        }
    }

    private async Task Reserve(string owner, Guid sessionId, Guid id, string name, string mediaType, CancellationToken ct)
    {
        await using var connection = await source.OpenConnectionAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);
        await using (var gate = new NpgsqlCommand("SELECT pg_advisory_xact_lock(hashtextextended($1,1))", connection, transaction))
        { gate.Parameters.AddWithValue(owner); await gate.ExecuteNonQueryAsync(ct); }
        await Expire(owner, ct);
        await using var quota = new NpgsqlCommand("""
            SELECT COALESCE(SUM(CASE WHEN d.status='uploading' THEN 10485760 ELSE COALESCE(d.byte_length,0) END),0),COUNT(*)
            FROM chat_documents d JOIN chat_sessions s ON s.id=d.session_id WHERE s.owner_subject=$1
            """, connection, transaction);
        quota.Parameters.AddWithValue(owner);
        await using (var reader = await quota.ExecuteReaderAsync(ct))
        {
            await reader.ReadAsync(ct);
            if (reader.GetDecimal(0) + MaxBytes > OwnerQuotaBytes || reader.GetInt64(1) >= 100)
                throw new DocumentLimitException("Account upload limit reached (100 MiB reserved/stored or 100 upload records).");
        }
        await using var insert = new NpgsqlCommand("""
            INSERT INTO chat_documents(id,session_id,filename,media_type,status)
            SELECT $1,id,$3,$4,'uploading' FROM chat_sessions WHERE id=$2 AND owner_subject=$5
            """, connection, transaction);
        insert.Parameters.AddWithValue(id); insert.Parameters.AddWithValue(sessionId); insert.Parameters.AddWithValue(name);
        insert.Parameters.AddWithValue(mediaType); insert.Parameters.AddWithValue(owner);
        if (await insert.ExecuteNonQueryAsync(ct) != 1) throw new ChatNotFoundException();
        await transaction.CommitAsync(ct);
    }

    public async Task DeleteAsync(ClaimsPrincipal user, Guid sessionId, Guid id, CancellationToken ct = default)
    {
        // One atomic delete removes the original and cascades to chunks/vectors. A worker
        // already processing this ID cannot publish because its fenced UPDATE finds no row.
        await using var cmd = source.CreateCommand("""
            DELETE FROM chat_documents d USING chat_sessions s
            WHERE d.session_id=s.id AND s.id=$1 AND s.owner_subject=$2 AND d.id=$3
            """);
        cmd.Parameters.AddWithValue(sessionId); cmd.Parameters.AddWithValue(Owner(user)); cmd.Parameters.AddWithValue(id);
        if (await cmd.ExecuteNonQueryAsync(ct) != 1) throw new ChatNotFoundException();
    }

    public async Task RetryAsync(ClaimsPrincipal user, Guid sessionId, Guid id, CancellationToken ct = default)
    {
        await GetAsync(user, sessionId, id, ct);
        await using var cmd = source.CreateCommand("UPDATE chat_documents d SET processing_status='queued',processing_attempts=0,processing_error=NULL,updated_at=clock_timestamp() FROM chat_sessions s WHERE d.session_id=s.id AND s.id=$1 AND s.owner_subject=$2 AND d.id=$3 AND d.status='uploaded' AND d.processing_status='failed'");
        cmd.Parameters.AddWithValue(sessionId); cmd.Parameters.AddWithValue(Owner(user)); cmd.Parameters.AddWithValue(id);
        if (await cmd.ExecuteNonQueryAsync(ct) != 1) throw new DocumentLimitException("Only failed processing can be retried.");
    }
    private async Task RequireSession(string owner, Guid sessionId, CancellationToken ct)
    {
        await using var cmd = source.CreateCommand("SELECT 1 FROM chat_sessions WHERE id=$1 AND owner_subject=$2");
        cmd.Parameters.AddWithValue(sessionId); cmd.Parameters.AddWithValue(owner);
        if (await cmd.ExecuteScalarAsync(ct) is null) throw new ChatNotFoundException();
    }
    private async Task Expire(string owner, CancellationToken ct)
    {
        await using var cmd = source.CreateCommand("""
            UPDATE chat_documents d SET status='failed',error_code='upload_interrupted',updated_at=clock_timestamp()
            FROM chat_sessions s WHERE s.id=d.session_id AND s.owner_subject=$1 AND d.status='uploading' AND d.upload_deadline <= clock_timestamp()
            """);
        cmd.Parameters.AddWithValue(owner); await cmd.ExecuteNonQueryAsync(ct);
    }
    private static string Owner(ClaimsPrincipal user)
    {
        string? id = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (user.Identity?.IsAuthenticated != true || string.IsNullOrWhiteSpace(id) || id.Length > 255) throw new UnauthorizedAccessException();
        return id;
    }
    private static ChatDocument Read(NpgsqlDataReader r) => new(r.GetGuid(0),r.GetGuid(1),r.GetString(2),r.GetString(3),r.GetString(4),
        r.IsDBNull(5) ? null : r.GetInt64(5),r.IsDBNull(6) ? null : r.GetString(6),r.IsDBNull(7) ? null : r.GetString(7),r.GetDateTime(8),r.GetDateTime(9),r.GetString(10),r.IsDBNull(11) ? null : r.GetString(11),r.GetInt32(12));
}

