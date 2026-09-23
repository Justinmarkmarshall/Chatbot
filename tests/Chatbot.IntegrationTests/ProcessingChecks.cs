using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Diagnostics;
using System.Globalization;
using Chatbot.Processing;
using Chatbot.Services;
using Npgsql;
using Microsoft.Extensions.Logging;
using UglyToad.PdfPig.Writer;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;

static class ProcessingChecks
{
    public static async Task Run(NpgsqlDataSource source, DocumentService documents, ClaimsPrincipal user, ClaimsPrincipal other, Guid session, Action<bool,string> check)
    {
        string root = Environment.GetEnvironmentVariable("CHATBOT_TEST_MODEL_ROOT") ?? Path.GetFullPath("experiments/embedding-spike/models");
        var watch = Stopwatch.StartNew();
        using var model = DocumentWorker.LoadModel(root);
        Console.WriteLine($"MiniLM load including verification: {watch.Elapsed.TotalMilliseconds:F1} ms");
        string fixture = Environment.GetEnvironmentVariable("CHATBOT_TEST_TOKEN_FIXTURE") ?? "experiments/embedding-spike/fixtures/minilm.json";
        using var refs = JsonDocument.Parse(File.ReadAllText(fixture));
        check(refs.RootElement.GetProperty("cases").EnumerateArray().All(c => model.Tokenize(c.GetProperty("text").GetString()!).SequenceEqual(c.GetProperty("ids").EnumerateArray().Select(x => x.GetInt64()))), "Promoted tokenizer matches all independent reference fixtures");
        var processor = new DocumentProcessor(model);
        string longText = "# Kubernetes\n" + string.Join(' ', Enumerable.Repeat("Kubernetes pods run containers managed by deployments.", 100)) + "\n# Baking\nBread dough rises when yeast ferments the flour.";
        watch.Restart();
        var prepared = processor.Prepare(Encoding.UTF8.GetBytes(longText), "text/plain");
        Console.WriteLine($"Prepare {prepared.Chunks.Count} chunks: {watch.Elapsed.TotalMilliseconds:F1} ms; process working set {Process.GetCurrentProcess().WorkingSet64/1048576d:F1} MiB");
        check(prepared.Chunks.All(c => c.Tokens <= 254 && c.Embedding.Length == 384 && prepared.Text[c.Start..c.End] == c.Content), "Chunks retain source offsets, <=254 tokens and 384 dimensions");
        var kube = prepared.Chunks.Where(c => c.Heading == "Kubernetes").ToArray();
        check(kube.Length > 1 && kube.Zip(kube.Skip(1)).All(pair => model.Tokenize(prepared.Text[pair.Second.Start..pair.First.End]).Length - 2 == 50), "Oversized heading sections use exactly 50-token overlap without truncation");
        check(prepared.Chunks.Last().Heading == "Baking" && prepared.Chunks.Last().Content.Contains("yeast"), "Heading boundaries and final text are retained");
        var builder = new PdfDocumentBuilder();
        var font = builder.AddStandard14Font(Standard14Font.Helvetica);
        var page = builder.AddPage(PageSize.A4);
        page.AddText("Kubernetes", 20, new PdfPoint(25, 750), font);
        page.AddText("Kubernetes pods run containers in a cluster.", 12, new PdfPoint(25, 710), font);
        var pdf = processor.Prepare(builder.Build(), "application/pdf");
        check(pdf.Chunks.Any(c => c.PageNumber == 1 && c.Heading == "Kubernetes" && c.Content.Contains("pods")), "Real PDF extraction retains page and detects large-font heading");
        bool emptyFailed = false;
        try { processor.Prepare(Encoding.UTF8.GetBytes(" \n"), "text/plain"); } catch (InvalidDataException) { emptyFailed = true; }
        check(emptyFailed, "Empty extracted text fails processing");
        var uploaded = await documents.UploadAsync(user, session, "retrieval.txt", new MemoryStream(Encoding.UTF8.GetBytes("# Kubernetes\nKubernetes deployments manage pods and containers.\n# Baking\nBread dough rises when yeast ferments flour.\n# Astronomy\nJupiter is a large gas giant planet orbiting the Sun.")));
        check(uploaded.ProcessingStatus == "queued", "Successful upload atomically queues processing");
        using var logs = LoggerFactory.Create(_ => { });
        var worker = new DocumentWorker(source, processor, logs.CreateLogger<DocumentWorker>());
        for (int i=0; i<110 && await worker.ProcessNextAsync(); i++) { }
        var done = await documents.GetAsync(user,session,uploaded.Id);
        check(done.ProcessingStatus == "ready" && done.ChunkCount == 3, "Worker persists all chunks and ready status together");
        string vector = "[" + string.Join(',',model.Embed("How do Kubernetes deployments manage containers?").Select(x=>x.ToString("R",CultureInfo.InvariantCulture))) + "]";
        await using var query = source.CreateCommand("SELECT c.heading,1-(c.embedding OPERATOR(public.<=>) $1::public.vector) FROM document_chunks c JOIN chat_documents d ON d.id=c.document_id JOIN chat_sessions s ON s.id=d.session_id WHERE d.id=$2 AND s.owner_subject=$3 AND d.processing_status='ready' ORDER BY c.embedding OPERATOR(public.<=>) $1::public.vector,c.ordinal");
        query.Parameters.AddWithValue(vector);query.Parameters.AddWithValue(uploaded.Id);query.Parameters.AddWithValue(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        watch.Restart();
        await using(var r = await query.ExecuteReaderAsync())
        {
            await r.ReadAsync(); check(r.GetString(0)=="Kubernetes", "Exact pgvector cosine query ranks relevant document section first");
            Console.WriteLine($"Top cosine similarity {r.GetDouble(1):F4}; query {watch.Elapsed.TotalMilliseconds:F1} ms");
        }
        query.Parameters[2].Value = other.FindFirst(ClaimTypes.NameIdentifier)!.Value;
        check(await query.ExecuteScalarAsync() is null,"Owner-scoped vector query excludes another account's chunks");
        await using(var reset=source.CreateCommand("UPDATE chat_documents SET processing_status='processing',processing_token=gen_random_uuid() WHERE id=$1")) { reset.Parameters.AddWithValue(uploaded.Id);await reset.ExecuteNonQueryAsync(); }
        await worker.ProcessNextAsync();
        check((await documents.GetAsync(user,session,uploaded.Id)).ChunkCount==3,"Abandoned processing recovers without duplicate chunks");
        await using (var gate = await source.OpenConnectionAsync())
        {
            await using var lockCommand = new NpgsqlCommand("SELECT pg_advisory_lock(734918260115)", gate);
            await lockCommand.ExecuteNonQueryAsync();
            check(!await worker.ProcessNextAsync(), "Another worker cannot process while global worker lock is held");
            await using var unlock = new NpgsqlCommand("SELECT pg_advisory_unlock(734918260115)", gate);
            await unlock.ExecuteNonQueryAsync();
        }
        bool denied = false;
        try { await documents.RetryAsync(other,session,uploaded.Id); } catch (Chatbot.Models.ChatNotFoundException) { denied = true; }
        check(denied, "Retry processing enforces document ownership");
        var empty = await documents.UploadAsync(user,session,"empty-text.txt",new MemoryStream(Encoding.UTF8.GetBytes("   \n")));
        await worker.ProcessNextAsync();
        check((await documents.GetAsync(user,session,empty.Id)).ProcessingError == "no_text", "Unsearchable original receives persistent no_text failure");
        await using(var partial = source.CreateCommand("SELECT count(*) FROM document_chunks WHERE document_id=$1"))
        {
            partial.Parameters.AddWithValue(empty.Id);
            check((long)(await partial.ExecuteScalarAsync())! == 0, "Failed processing leaves no partial chunks");
        }
        var failed=(await documents.ListAsync(user,session)).FirstOrDefault(d=>d.ProcessingStatus=="failed");
        if(failed is not null)
        {
            check((await documents.DownloadAsync(user,session,failed.Id)).Content.Length>0,"Processing failure preserves original download");
            await documents.RetryAsync(user,session,failed.Id);
            check((await documents.GetAsync(user,session,failed.Id)).ProcessingStatus=="queued","Failed processing can be retried");
        }
    }
}

