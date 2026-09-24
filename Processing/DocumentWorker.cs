using System.Globalization;
using System.Text.Json;
using Chatbot.Persistence;
using Npgsql;

namespace Chatbot.Processing;

// A separate process serializes jobs with one PostgreSQL session advisory lock.
// A random fencing token prevents a disconnected, stale worker publishing a result.
public sealed class DocumentWorker(NpgsqlDataSource source, DocumentProcessor processor, ILogger<DocumentWorker> logger) : BackgroundService
{
    public async Task<bool> ProcessNextAsync(CancellationToken ct = default)
    {
        await using var gate = await source.OpenConnectionAsync(ct);
        await using var acquire = new NpgsqlCommand("SELECT pg_try_advisory_lock(734918260115)", gate);
        if (!(bool)(await acquire.ExecuteScalarAsync(ct))!) return false;
        try
        {
            Guid id, token = Guid.NewGuid(); byte[] original; string media;
            await using (var claim = source.CreateCommand("""
                UPDATE chat_documents SET processing_status='processing',processing_token=$1,
                  processing_attempts=processing_attempts+1,processing_error=NULL,updated_at=clock_timestamp()
                WHERE id=(SELECT id FROM chat_documents WHERE status='uploaded'
                  AND processing_status IN ('queued','processing') ORDER BY created_at,id LIMIT 1)
                RETURNING id,original,media_type,processing_attempts
                """))
            {
                claim.Parameters.AddWithValue(token);
                await using var r = await claim.ExecuteReaderAsync(ct);
                if (!await r.ReadAsync(ct)) return false;
                id = r.GetGuid(0); original = r.GetFieldValue<byte[]>(1); media = r.GetString(2);
                if (r.GetInt32(3) > 3) { await Fail(id, token, "interrupted_repeatedly", ct); return true; }
            }
            try
            {
                using var deadline = CancellationTokenSource.CreateLinkedTokenSource(ct);
                deadline.CancelAfter(TimeSpan.FromMinutes(5));
                var prepared = processor.Prepare(original, media, deadline.Token);
                await Publish(id, token, prepared, deadline.Token);
                logger.LogInformation("Processed document {DocumentId}: {Count} chunks", id, prepared.Chunks.Count);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception ex)
            {
                // Never persist parser exception text or document content into user-visible errors/logs.
                string code = ex is InvalidDataException && ex.Message is "page_limit" or "text_limit" or "chunk_limit" or "no_text" ? ex.Message : "processing_failed";
                await Fail(id, token, code, ct);
                logger.LogWarning("Document {DocumentId} processing failed ({Error}, {ExceptionType})", id, code, ex.GetType().Name);
            }
            return true;
        }
        finally
        {
            await using var release = new NpgsqlCommand("SELECT pg_advisory_unlock(734918260115)", gate);
            await release.ExecuteNonQueryAsync(CancellationToken.None);
        }
    }
    private async Task Fail(Guid id, Guid token, string code, CancellationToken ct)
    {
        await using var cmd = source.CreateCommand("UPDATE chat_documents SET processing_status='failed',processing_error=$3,processing_token=NULL,updated_at=clock_timestamp() WHERE id=$1 AND processing_token=$2");
        cmd.Parameters.AddWithValue(id); cmd.Parameters.AddWithValue(token); cmd.Parameters.AddWithValue(code);
        await cmd.ExecuteNonQueryAsync(ct);
    }
    private async Task Publish(Guid id, Guid token, PreparedDocument result, CancellationToken ct)
    {
        await using var connection = await source.OpenConnectionAsync(ct);
        await using var tx = await connection.BeginTransactionAsync(ct);
        await using var finish = new NpgsqlCommand("UPDATE chat_documents SET processing_status='ready',processing_token=NULL,processing_error=NULL,extracted_text=$3,chunk_count=$4,updated_at=clock_timestamp() WHERE id=$1 AND processing_token=$2 AND processing_status='processing'", connection, tx);
        finish.Parameters.AddWithValue(id); finish.Parameters.AddWithValue(token); finish.Parameters.AddWithValue(result.Text.Replace("\0", string.Empty)); finish.Parameters.AddWithValue(result.Chunks.Count);
        if (await finish.ExecuteNonQueryAsync(ct) != 1) throw new InvalidOperationException("Worker lost document ownership.");
        await using var delete = new NpgsqlCommand("DELETE FROM document_chunks WHERE document_id=$1", connection, tx);
        delete.Parameters.AddWithValue(id); await delete.ExecuteNonQueryAsync(ct);
        for (int index = 0; index < result.Chunks.Count; index++)
        {
            var chunk = result.Chunks[index];
            await using var insert = new NpgsqlCommand("""
                INSERT INTO document_chunks(document_id,ordinal,heading,page_number,start_offset,end_offset,content,token_count,embedding,model_profile)
                VALUES($1,$2,$3,$4,$5,$6,$7,$8,$9::public.vector,$10)
                """, connection, tx);
            insert.Parameters.AddWithValue(id); insert.Parameters.AddWithValue(index); insert.Parameters.AddWithValue(chunk.Heading.Replace("\0", string.Empty));
            insert.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Integer, Value = (object?)chunk.PageNumber ?? DBNull.Value });
            insert.Parameters.AddWithValue(chunk.Start); insert.Parameters.AddWithValue(chunk.End); insert.Parameters.AddWithValue(chunk.Content.Replace("\0", string.Empty)); insert.Parameters.AddWithValue(chunk.Tokens);
            // Parameterized invariant text cast avoids loading vector type mappings before migration.
            insert.Parameters.AddWithValue("[" + string.Join(',', chunk.Embedding.Select(x => x.ToString("R", CultureInfo.InvariantCulture))) + "]");
            insert.Parameters.AddWithValue(DocumentProcessor.Profile); await insert.ExecuteNonQueryAsync(ct);
        }
        await tx.CommitAsync(ct);
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try { if (await ProcessNextAsync(stoppingToken)) continue; }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { logger.LogError("Document worker database failure ({ExceptionType})", ex.GetType().Name); }
            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }
    public static OnnxEmbedder LoadModel(string root)
    {
        using var manifest = typeof(DocumentWorker).Assembly.GetManifestResourceStream("Chatbot.Processing.minilm.json")!;
        var spec = JsonSerializer.Deserialize<ModelSpec>(manifest, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        return new(spec, root);
    }
}
