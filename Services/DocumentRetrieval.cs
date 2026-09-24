using System.Diagnostics;
using System.Globalization;
using Chatbot.Models;
using Chatbot.Processing;
using Npgsql;

namespace Chatbot.Services;

public sealed class DocumentRetrieval(NpgsqlDataSource source, IQueryEmbedding embedding, ILogger<DocumentRetrieval>? logger = null) : IDocumentRetrieval
{
    // owner comes exclusively from ChatService's authenticated NameIdentifier, never request data.
    public async Task<DocumentEvidence> RetrieveAsync(string owner, Guid sessionId, string question, CancellationToken ct)
    {
        var total = Stopwatch.StartNew();
        var embeddingTime = new Stopwatch();
        var queryTime = new Stopwatch();
        int retrievedCount = 0;
        try
        {
            await using var state = source.CreateCommand("""
                SELECT EXISTS(SELECT 1 FROM chat_documents d WHERE d.session_id=s.id AND d.status='uploaded'),
                       EXISTS(SELECT 1 FROM chat_documents d WHERE d.session_id=s.id AND d.status='uploaded' AND d.processing_status='ready')
                FROM chat_sessions s WHERE s.id=$1 AND s.owner_subject=$2
                """);
            state.Parameters.AddWithValue(sessionId); state.Parameters.AddWithValue(owner);
            await using (var reader = await state.ExecuteReaderAsync(ct))
            {
                if (!await reader.ReadAsync(ct)) throw new ChatNotFoundException();
                bool uploads = reader.GetBoolean(0), ready = reader.GetBoolean(1);
                if (!ready) return new(uploads, []);
            }
            embeddingTime.Start();
            float[] vector;
            try { vector = await embedding.EmbedAsync(question, ct); }
            finally { embeddingTime.Stop(); }
            if (vector.Length != 384 || vector.Any(v => !float.IsFinite(v)) || vector.All(v => v == 0))
                throw new InvalidDataException("Invalid MiniLM query embedding.");
            await using var query = source.CreateCommand("""
                SELECT d.id,d.filename,c.heading,c.ordinal,c.page_number,c.content,
                       c.embedding OPERATOR(public.<=>) $3::public.vector AS distance
                FROM document_chunks c
                JOIN chat_documents d ON d.id=c.document_id
                JOIN chat_sessions s ON s.id=d.session_id
                WHERE s.id=$1 AND s.owner_subject=$2 AND d.status='uploaded'
                  AND d.processing_status='ready' AND c.model_profile=$4
                ORDER BY c.embedding OPERATOR(public.<=>) $3::public.vector,d.id,c.ordinal
                LIMIT 5
                """);
            query.Parameters.AddWithValue(sessionId); query.Parameters.AddWithValue(owner);
            query.Parameters.AddWithValue("[" + string.Join(',', vector.Select(v => v.ToString("R", CultureInfo.InvariantCulture))) + "]");
            query.Parameters.AddWithValue(DocumentProcessor.Profile);
            var sources = new List<DocumentSource>();
            queryTime.Start();
            await using var results = await query.ExecuteReaderAsync(ct);
            while (await results.ReadAsync(ct))
                sources.Add(new(sources.Count + 1, results.GetGuid(0), results.GetString(1), results.GetString(2), results.GetInt32(3),
                    results.IsDBNull(4) ? null : results.GetInt32(4), results.GetString(5), results.GetDouble(6)));
            retrievedCount = sources.Count;
            return new(true, sources);
        }
        finally
        {
            queryTime.Stop();
            logger?.LogInformation("Document retrieval for chat {ChatId}: {RetrievedCount} chunks, embedding {EmbeddingMs} ms, vector query {QueryMs} ms, retrieval total {RetrievalMs} ms",
                sessionId, retrievedCount, embeddingTime.Elapsed.TotalMilliseconds, queryTime.Elapsed.TotalMilliseconds, total.Elapsed.TotalMilliseconds);
        }
    }
}
