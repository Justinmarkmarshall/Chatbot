using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using Npgsql;
using Pgvector;
using Pgvector.Npgsql;

var json = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, WriteIndented = true };
var failures = new List<string>();
void Check(bool condition, string message) { if (!condition && !failures.Contains(message)) failures.Add(message); }
void Require(bool condition, string message) { if (!condition) throw new InvalidDataException(message); }
string Hash(string path) => Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(path)));
var inputLock = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText("inputs.lock.json"), json)!;
foreach (var (path, expected) in inputLock) Require(Hash(path) == expected, $"Frozen input changed: {path}");
var manifest = JsonSerializer.Deserialize<Manifest>(File.ReadAllText("corpus/manifest.json"), json)!;
var cases = JsonSerializer.Deserialize<Case[]>(File.ReadAllText("cases.json"), json)!;
var documents = manifest.Documents.Select(d =>
{
    string text = File.ReadAllText(Path.Combine("corpus", d.File));
    var headings = Regex.Matches(text, @"(?m)^#{1,6} (.+)\r?$").Cast<Match>().ToArray();
    Require(headings.Length > 1 && headings[0].Index == 0, "Corpus requires leading title and sections");
    return new SourceDocument(d.Id, d.File, text, headings.Select((h, i) =>
        new Section(h.Groups[1].Value.Trim(), h.Index, i + 1 < headings.Length ? headings[i + 1].Index : text.Length)).ToArray());
}).ToArray();
int words = documents.Sum(d => Regex.Matches(d.Text, @"\S+").Count);
Require(documents.Length is >= 8 and <= 12 && words is >= 5000 and <= 15000, "Corpus size");
Require(cases.Length is >= 20 and <= 30 && cases.Any(c => !c.Answerable), "Case counts");
Require(cases.Select(c => c.Id).Distinct().Count() == cases.Length, "Unique case IDs");
// Resolve frozen semantic labels and verbatim evidence to source spans BEFORE retrieval.
var evidence = new Dictionary<string, Span[]>();
foreach (var c in cases.Where(c => c.Answerable))
{
    var d = documents.Single(d => d.Id == c.ExpectedDocument);
    var section = d.Sections.Single(s => s.Heading == c.ExpectedSection);
    Require(c.Evidence.Length > 0, "Answerable case requires evidence");
    evidence[c.Id] = c.Evidence.Select(e =>
    {
        int start = d.Text.IndexOf(e, section.Start, section.End - section.Start, StringComparison.Ordinal);
        Require(start >= 0, $"Evidence not in expected section: {c.Id}");
        Require(d.Text.IndexOf(e, start + 1, StringComparison.Ordinal) < 0, $"Ambiguous evidence occurrence: {c.Id}");
        return new Span(start, start + e.Length);
    }).ToArray();
}
File.WriteAllText("/results/frozen-inputs.json", JsonSerializer.Serialize(inputLock, json));
using var regression = JsonDocument.Parse(File.ReadAllText("/results/embedding-regression-linux.json"));
Require(regression.RootElement.GetProperty("Passed").GetBoolean(), "Original MiniLM regression must pass");
var spec = JsonSerializer.Deserialize<ModelSpec[]>(File.ReadAllText("models.json"), json)!.Single(s => s.Id == "minilm");
Require(spec.MaxTokens == 256 && spec.Precision == "FP32" && regression.RootElement.GetProperty("Revision").GetString() == spec.Revision,
    "Pinned MiniLM baseline");
var startup = Stopwatch.StartNew();
using var embedder = new OnnxEmbedder(spec, "/models");
startup.Stop();
var windows = new TokenWindows(embedder);
string[] strategies = ["paragraph", "fixed-254-50", "heading-aware"];
Chunk[] Split(SourceDocument d, string strategy) => strategy switch
{
    "paragraph" => ParagraphChunker.Run(d, windows),
    "fixed-254-50" => FixedTokenChunker.Run(d, windows),
    "heading-aware" => HeadingAwareChunker.Run(d, windows),
    _ => throw new ArgumentException(strategy)
};
void ValidateChunks(SourceDocument d, Chunk[] chunks)
{
    Check(chunks.Length > 0, "Nonempty chunks");
    var covered = new bool[d.Text.Length];
    foreach (var c in chunks)
    {
        Check(c.SourceText == d.Text[c.StartOffset..c.EndOffset], "Exact source span roundtrip");
        Check(c.TokenCount is > 0 and <= 254 && embedder.Tokenize(c.SourceText).Length == c.TokenCount + 2, "Token cap and special tokens");
        for (int i = c.StartOffset; i < c.EndOffset; i++) covered[i] = true;
    }
    Check(Enumerable.Range(0, d.Text.Length).All(i => char.IsWhiteSpace(d.Text[i]) || covered[i]), "No non-whitespace source lost");
    for (int i = 1; i < chunks.Length; i++)
    {
        var previous = chunks[i - 1]; var current = chunks[i];
        Check(current.StartOffset > previous.StartOffset, "Chunk progress/order");
        if (current.StartOffset < previous.EndOffset)
            Check(windows.Count(d.Text[current.StartOffset..previous.EndOffset]) == 50, "Exact 50-token subdivision overlap");
    }
}
// A genuinely oversized paragraph/section exercises subdivision for every strategy.
string synthetic = "# Synthetic\n\n" + string.Join(" ", Enumerable.Repeat("The garden team records the soil moisture before watering the plants.", 90));
var fixture = new SourceDocument("test", "not-corpus", synthetic, [new("Synthetic", 0, synthetic.Length)]);
foreach (string strategy in strategies)
{
    var split = Split(fixture, strategy);
    Check(split.Length > 1, "Oversized fixture subdivided");
    ValidateChunks(fixture, split);
    Check(split.SequenceEqual(Split(fixture, strategy)), "Synthetic deterministic chunking");
}
var chunking = Stopwatch.StartNew();
var chunks = new List<Chunk>();
foreach (string strategy in strategies)
    foreach (var d in documents)
    {
        var split = Split(d, strategy);
        ValidateChunks(d, split);
        Check(split.SequenceEqual(Split(d, strategy)), "Corpus deterministic chunking");
        chunks.AddRange(split);
    }
chunking.Stop();
Require(failures.Count == 0, string.Join("; ", failures));
Console.WriteLine($"Frozen corpus: {documents.Length} documents, {words} words, {cases.Length} questions; {chunks.Count} chunks.");
var corpusTimer = Stopwatch.StartNew();
var embeddings = chunks.Select(c => embedder.Embed(c.SourceText)).ToArray();
corpusTimer.Stop();
var builder = new NpgsqlDataSourceBuilder(Environment.GetEnvironmentVariable("SPIKE_CONNECTION_STRING") ?? throw new InvalidOperationException("Connection required"));
builder.UseVector();
await using var dataSource = builder.Build();
await using var connection = await dataSource.OpenConnectionAsync();
async Task<string> Scalar(string sql)
{
    await using var command = new NpgsqlCommand(sql, connection);
    return (await command.ExecuteScalarAsync())?.ToString() ?? "";
}
string pgVersion = await Scalar("SELECT version()"), vectorVersion = await Scalar("SELECT extversion FROM pg_extension WHERE extname='vector'");
Require(vectorVersion == "0.8.6", "Pinned pgvector extension");
var byId = new Dictionary<int, Chunk>();
var seedById = new Dictionary<int, float[]>();
var insertion = Stopwatch.StartNew();
await using (var transaction = await connection.BeginTransactionAsync())
{
    await using (var clear = new NpgsqlCommand("TRUNCATE document_chunks RESTART IDENTITY", connection, transaction)) await clear.ExecuteNonQueryAsync();
    for (int i = 0; i < chunks.Count; i++)
    {
        var c = chunks[i];
        await using var insert = new NpgsqlCommand("INSERT INTO document_chunks(document_id,chunk_strategy,chunk_index,heading,source_text,token_count,start_offset,end_offset,embedding) VALUES($1,$2,$3,$4,$5,$6,$7,$8,$9) RETURNING id", connection, transaction);
        insert.Parameters.AddWithValue(c.DocumentId); insert.Parameters.AddWithValue(c.Strategy); insert.Parameters.AddWithValue(c.ChunkIndex);
        insert.Parameters.AddWithValue(c.Heading ?? ""); insert.Parameters.AddWithValue(c.SourceText); insert.Parameters.AddWithValue(c.TokenCount);
        insert.Parameters.AddWithValue(c.StartOffset); insert.Parameters.AddWithValue(c.EndOffset); insert.Parameters.AddWithValue(new Vector(embeddings[i]));
        int id = (int)(await insert.ExecuteScalarAsync())!;
        byId.Add(id, c); seedById.Add(id, embeddings[i]);
    }
    await transaction.CommitAsync();
}
insertion.Stop();
var stored = new Dictionary<int, float[]>();
await using (var cmd = new NpgsqlCommand("SELECT id,document_id,chunk_strategy,chunk_index,heading,source_text,token_count,start_offset,end_offset,embedding FROM document_chunks ORDER BY id", connection))
await using (var reader = await cmd.ExecuteReaderAsync())
    while (await reader.ReadAsync())
    {
        int id = reader.GetInt32(0);
        var readChunk = new Chunk(reader.GetString(1), reader.GetString(2), reader.GetInt32(3), reader.GetString(4), reader.GetString(5), reader.GetInt32(6), reader.GetInt32(7), reader.GetInt32(8));
        var vector = reader.GetFieldValue<Vector>(9).ToArray();
        stored[id] = vector;
        Check(vector.Length == 384 && vector.All(float.IsFinite) && vector.SequenceEqual(seedById[id]), "Finite 384-dimensional float32 vector roundtrip");
        Check(readChunk == byId[id], "All provenance fields roundtrip");
    }
Check(stored.Count == chunks.Count, "All chunks stored");
var indexes = new List<IndexInfo>();
await using (var cmd = new NpgsqlCommand("SELECT indexname,indexdef FROM pg_indexes WHERE schemaname='public' AND tablename='document_chunks' ORDER BY indexname", connection))
await using (var reader = await cmd.ExecuteReaderAsync())
    while (await reader.ReadAsync()) indexes.Add(new(reader.GetString(0), reader.GetString(1)));
Check(indexes.Count == 2 && indexes.All(i => i.Definition.Contains("USING btree") && !i.Definition.Contains("embedding")), "Only primary key and uniqueness B-tree indexes");
Check(indexes.All(i => !Regex.IsMatch(i.Definition, "hnsw|ivfflat", RegexOptions.IgnoreCase)), "No approximate index");
await Scalar("ANALYZE document_chunks");
var searchSql = File.ReadAllText("search.sql");
async Task<(List<Hit> Hits, double Ms)> Search(Vector query, string strategy, int limit)
{
    await using var command = new NpgsqlCommand(searchSql, connection);
    command.Parameters.AddWithValue(query); command.Parameters.AddWithValue(strategy); command.Parameters.AddWithValue(limit);
    var timer = Stopwatch.StartNew();
    await using var reader = await command.ExecuteReaderAsync();
    var hits = new List<Hit>();
    while (await reader.ReadAsync())
        hits.Add(new(hits.Count + 1, reader.GetInt32(0), reader.GetString(1), reader.GetInt32(2), reader.GetString(3), reader.GetString(4),
            reader.GetDouble(5), reader.GetDouble(6), reader.GetInt32(7), reader.GetInt32(8), reader.GetInt32(9)));
    timer.Stop();
    return (hits, timer.Elapsed.TotalMilliseconds);
}
bool Relevant(Case c, Hit hit) => c.Answerable && hit.DocumentId == c.ExpectedDocument &&
    evidence[c.Id].All(s => hit.StartOffset <= s.Start && hit.EndOffset >= s.End);
var reports = new List<StrategyReport>();
double maxDifference = 0;
int comparisons = 0;
foreach (string strategy in strategies)
{
    var questions = new List<QuestionReport>();
    int count = chunks.Count(c => c.Strategy == strategy);
    foreach (var c in cases)
    {
        var query = embedder.Embed(c.Question);
        var (ranking, _) = await Search(new Vector(query), strategy, count);
        Check(ranking.Count == count && ranking.Select(h => h.Id).Distinct().Count() == count, "Complete unique ranking");
        Check(ranking.All(h => byId[h.Id].Strategy == strategy), "No cross-strategy results");
        Check(ranking.Select(h => h.Id).SequenceEqual(ranking.OrderBy(h => h.CosineDistance).ThenBy(h => h.Id).Select(h => h.Id)), "Distance and ID order");
        foreach (var hit in ranking)
        {
            double error = Math.Abs(CosineDistance(query, stored[hit.Id]) - hit.CosineDistance);
            maxDifference = Math.Max(maxDifference, error); comparisons++;
            Check(error <= 1e-5, "Independent double cosine tolerance");
            Check(Math.Abs(hit.Similarity - (1 - hit.CosineDistance)) < 1e-12, "Similarity equals one minus distance");
        }
        var relevant = ranking.Where(h => Relevant(c, h)).ToArray();
        var first = relevant.FirstOrDefault();
        var irrelevant = ranking.FirstOrDefault(h => !Relevant(c, h));
        var plan = new List<string>();
        if (c.Id == cases[0].Id)
        {
            await using var explain = new NpgsqlCommand("EXPLAIN (ANALYZE, BUFFERS) " + searchSql, connection);
            explain.Parameters.AddWithValue(new Vector(query)); explain.Parameters.AddWithValue(strategy); explain.Parameters.AddWithValue(5);
            await using var reader = await explain.ExecuteReaderAsync();
            while (await reader.ReadAsync()) plan.Add(reader.GetString(0));
        }
        var samples = new List<Sample>();
        for (int i = 0; i < 55; i++)
        {
            var endToEnd = Stopwatch.StartNew(); var embeddingTimer = Stopwatch.StartNew();
            var vector = embedder.Embed(c.Question);
            embeddingTimer.Stop();
            var (hits, databaseMs) = await Search(new Vector(vector), strategy, 5);
            endToEnd.Stop();
            if (i >= 5) samples.Add(new(embeddingTimer.Elapsed.TotalMilliseconds, databaseMs, endToEnd.Elapsed.TotalMilliseconds));
            Check(hits.Select(h => h.Id).SequenceEqual(ranking.Take(5).Select(h => h.Id)), "Measured top-five ranking stable");
        }
        // Classification is a diagnostic, never a change to frozen relevance labels.
        string? diagnosis = c.Answerable && first?.Rank != 1 ? relevant.Length == 0 ? "boundary split" : "unknown" : null;
        questions.Add(new(c, c.Answerable ? evidence[c.Id] : [], relevant.Select(h => h.Id).ToArray(), first?.Rank, first?.Similarity,
            irrelevant?.Similarity, diagnosis, ranking, plan, Stats(samples.Select(s => s.EmbeddingMs)), Stats(samples.Select(s => s.DatabaseMs)),
            Stats(samples.Select(s => s.EndToEndMs)), samples));
    }
    var answerable = questions.Where(q => q.Case.Answerable).ToArray();
    var allSamples = questions.SelectMany(q => q.Samples).ToArray();
    var tokens = chunks.Where(c => c.Strategy == strategy).Select(c => (double)c.TokenCount).ToArray();
    var summary = new StrategyReport(strategy, count, tokens.Min(), Stats(tokens).Median, Stats(tokens).P95, tokens.Max(),
        answerable.Count(q => q.FirstRelevantRank is <= 1) / (double)answerable.Length,
        answerable.Count(q => q.FirstRelevantRank is <= 3) / (double)answerable.Length,
        answerable.Count(q => q.FirstRelevantRank is <= 5) / (double)answerable.Length,
        answerable.Average(q => q.FirstRelevantRank is int rank ? 1.0 / rank : 0),
        Stats(allSamples.Select(s => s.EmbeddingMs)), Stats(allSamples.Select(s => s.DatabaseMs)), Stats(allSamples.Select(s => s.EndToEndMs)), questions);
    reports.Add(summary);
    Console.WriteLine($"{strategy}: chunks={count}, recall@1={summary.Recall1:P1}, recall@3={summary.Recall3:P1}, MRR={summary.Mrr:F4}");
}
using var process = Process.GetCurrentProcess();
var report = new
{
    TimestampUtc = DateTimeOffset.UtcNow, ValidExperiment = failures.Count == 0, Failures = failures,
    QualityTargetReached = reports.Any(r => r.Recall3 >= 0.9), DocumentCount = documents.Length, WordCount = words,
    QuestionCount = cases.Length, AnswerableCount = cases.Count(c => c.Answerable), NoAnswerCount = cases.Count(c => !c.Answerable),
    Model = spec, Dimensions = 384, ExactSearch = true, FrozenInputs = inputLock,
    OriginalMiniLmRegressionPassed = true, RegressionFixtures = regression.RootElement.GetProperty("TokenizerFixtures").GetInt32(),
    ModelStartupMs = startup.Elapsed.TotalMilliseconds, embedder.ArtifactVerificationMs, embedder.TokenizerLoadMs, embedder.ModelLoadMs,
    ChunkingAndValidationMs = chunking.Elapsed.TotalMilliseconds, FullCorpusEmbeddingMs = corpusTimer.Elapsed.TotalMilliseconds,
    InsertMs = insertion.Elapsed.TotalMilliseconds, TotalChunksInserted = stored.Count,
    MaximumCosineDistanceDifference = maxDifference, CosineComparisons = comparisons, Indexes = indexes, PostgresVersion = pgVersion, PgvectorVersion = vectorVersion,
    Runtime = RuntimeInformation.FrameworkDescription, OS = RuntimeInformation.OSDescription, Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
    ProcessPeakRssBytes = process.PeakWorkingSet64, ContentTokenLimit = 254, SpecialTokens = 2, SubdivisionOverlap = 50,
    TopK = 5, WarmupsPerQuestionPerStrategy = 5, SamplesPerQuestionPerStrategy = 50, Connections = 1, DataSources = 1,
    ConnectionEstablishedOutsideTiming = true, TimedStrategyOrder = strategies, CorpusEmbeddingOrder = "strategy, manifest document, chunk index",
    ExactQuery = searchSql, Chunks = byId.Select(p => new { Id = p.Key, Chunk = p.Value }), Strategies = reports
};
File.WriteAllText("/results/retrieval.json", JsonSerializer.Serialize(report, json));
foreach (var failure in failures) Console.Error.WriteLine(failure);
Console.WriteLine($"Valid={report.ValidExperiment}; quality target={report.QualityTargetReached}; cosine error={maxDifference:G9}");
return report.ValidExperiment ? 0 : 1;

static double CosineDistance(float[] a, float[] b)
{
    double dot = 0, aa = 0, bb = 0;
    for (int i = 0; i < a.Length; i++) { dot += (double)a[i] * b[i]; aa += (double)a[i] * a[i]; bb += (double)b[i] * b[i]; }
    return 1 - dot / Math.Sqrt(aa * bb);
}
static Distribution Stats(IEnumerable<double> values)
{
    var a = values.Order().ToArray(); int n = a.Length;
    return new(n, n % 2 == 0 ? (a[n / 2 - 1] + a[n / 2]) / 2 : a[n / 2], a[(int)Math.Ceiling(0.95 * n) - 1]);
}
record Manifest(string CorpusVersion, DocumentEntry[] Documents);
record DocumentEntry(string Id, string File);
record Case(string Id, string Kind, string Question, bool Answerable, string? ExpectedDocument, string? ExpectedSection, string[] Evidence);
record Hit(int Rank, int Id, string DocumentId, int ChunkIndex, string Heading, string SourceText, double CosineDistance, double Similarity,
    int StartOffset, int EndOffset, int TokenCount);
record IndexInfo(string Name, string Definition);
record Sample(double EmbeddingMs, double DatabaseMs, double EndToEndMs);
record Distribution(int Count, double Median, double P95);
record QuestionReport(Case Case, Span[] EvidenceSpans, int[] RelevantIds, int? FirstRelevantRank, double? FirstRelevantSimilarity,
    double? HighestIrrelevantSimilarity, string? Diagnosis, List<Hit> FullRanking, List<string> ExplainAnalyzeBuffers,
    Distribution Embedding, Distribution Database, Distribution EndToEnd, List<Sample> Samples);
record StrategyReport(string Strategy, int ChunkCount, double MinTokens, double MedianTokens, double P95Tokens, double MaxTokens,
    double Recall1, double Recall3, double Recall5, double Mrr, Distribution Embedding, Distribution Database, Distribution EndToEnd, List<QuestionReport> Questions);
