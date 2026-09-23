using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using Npgsql;
using Pgvector;
using Pgvector.Npgsql;

// Fixed, disposable experiment. No application persistence abstractions.
var json = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, WriteIndented = true };
var failures = new List<string>();
void Check(bool condition, string message) { if (!condition) failures.Add(message); }
var regression = JsonDocument.Parse(File.ReadAllText("/results/embedding-regression-linux.json"));
if (!regression.RootElement.GetProperty("Passed").GetBoolean() ||
    regression.RootElement.GetProperty("Model").GetString() != "minilm")
    throw new InvalidOperationException("Original MiniLM regression must pass before this spike is valid.");
var specs = JsonSerializer.Deserialize<ModelSpec[]>(File.ReadAllText("models.json"), json)!;
var spec = specs.Single(x => x.Id == "minilm");
Check(regression.RootElement.GetProperty("Revision").GetString() == spec.Revision, "Regression model revision");
var startup = Stopwatch.StartNew();
using var embedder = new OnnxEmbedder(spec, "/models");
startup.Stop();
var cases = JsonSerializer.Deserialize<Cases>(File.ReadAllText("cases.json"), json)!;
var sql = File.ReadAllText("search.sql");
Check(cases.Passages.Length == 8 && cases.Passages.Select(x => x.Id).Order().SequenceEqual(Enumerable.Range(1, 8)), "Exactly eight seed IDs");
Check(cases.Questions.Count(x => x.ExpectedId.HasValue) == 6 && cases.Questions.Count(x => !x.ExpectedId.HasValue) == 1, "Fixed question counts");
var builder = new NpgsqlDataSourceBuilder(Environment.GetEnvironmentVariable("SPIKE_CONNECTION_STRING")
    ?? throw new InvalidOperationException("SPIKE_CONNECTION_STRING is required"));
builder.UseVector();
await using var dataSource = builder.Build();
await using var connection = await dataSource.OpenConnectionAsync();
async Task<string> Scalar(string command)
{
    await using var cmd = new NpgsqlCommand(command, connection);
    return (await cmd.ExecuteScalarAsync())?.ToString() ?? "";
}
var extensionVersion = await Scalar("SELECT extversion FROM pg_extension WHERE extname = 'vector'");
Check(extensionVersion == "0.8.6", "Expected pgvector 0.8.6 installed by initialization script");
if (string.IsNullOrEmpty(extensionVersion)) throw new InvalidOperationException("Missing vector extension");
var postgresVersion = await Scalar("SELECT version()");

// Generate real embeddings before opening the seed transaction.
var seed = cases.Passages.ToDictionary(x => x.Id, x => embedder.Embed(x.Text));
await using (var transaction = await connection.BeginTransactionAsync())
{
    await using (var clear = new NpgsqlCommand("TRUNCATE spike_passages", connection, transaction)) await clear.ExecuteNonQueryAsync();
    foreach (var passage in cases.Passages)
    {
        await using var insert = new NpgsqlCommand("INSERT INTO spike_passages (id, source_text, embedding) VALUES ($1, $2, $3)", connection, transaction);
        insert.Parameters.AddWithValue(passage.Id);
        insert.Parameters.AddWithValue(passage.Text);
        insert.Parameters.AddWithValue(new Vector(seed[passage.Id]));
        await insert.ExecuteNonQueryAsync();
    }
    await transaction.CommitAsync();
}
var stored = new Dictionary<int, float[]>();
await using (var cmd = new NpgsqlCommand("SELECT id, source_text, embedding, vector_dims(embedding) FROM spike_passages ORDER BY id", connection))
await using (var reader = await cmd.ExecuteReaderAsync())
{
    while (await reader.ReadAsync())
    {
        int id = reader.GetInt32(0);
        var vector = reader.GetFieldValue<Vector>(2).ToArray();
        stored.Add(id, vector);
        Check(reader.GetInt32(3) == 384 && vector.Length == 384 && vector.All(float.IsFinite), $"Stored embedding {id}: finite and 384-dimensional");
        Check(reader.GetString(1) == cases.Passages.Single(x => x.Id == id).Text, $"Source text roundtrip {id}");
        Check(vector.SequenceEqual(seed[id]), $"Vector float32 roundtrip {id}");
    }
}
Check(stored.Count == 8, "Eight stored embeddings");

var indexes = new List<IndexInfo>();
await using (var cmd = new NpgsqlCommand("SELECT indexname, indexdef FROM pg_indexes WHERE schemaname = 'public' AND tablename = 'spike_passages' ORDER BY indexname", connection))
await using (var reader = await cmd.ExecuteReaderAsync())
    while (await reader.ReadAsync()) indexes.Add(new(reader.GetString(0), reader.GetString(1)));
Check(indexes.Count == 1 && indexes[0].Name == "spike_passages_pkey" && indexes[0].Definition.Contains("USING btree (id)"), "Only primary-key index exists");
Check(indexes.All(x => !x.Definition.Contains("hnsw", StringComparison.OrdinalIgnoreCase) && !x.Definition.Contains("ivfflat", StringComparison.OrdinalIgnoreCase)), "No approximate vector index");

async Task<(List<Hit> Hits, double Ms)> Search(Vector query, int limit)
{
    await using var command = new NpgsqlCommand(sql, connection);
    command.Parameters.AddWithValue(query);
    command.Parameters.AddWithValue(limit);
    var timer = Stopwatch.StartNew();
    await using var reader = await command.ExecuteReaderAsync();
    var hits = new List<Hit>();
    while (await reader.ReadAsync())
        hits.Add(new(hits.Count + 1, reader.GetInt32(0), reader.GetString(1), reader.GetDouble(2), reader.GetDouble(3)));
    timer.Stop();
    return (hits, timer.Elapsed.TotalMilliseconds);
}

var reports = new List<QuestionReport>();
var allSamples = new List<Sample>();
double maxDifference = 0;
int top1Passed = 0;
foreach (var question in cases.Questions)
{
    var queryEmbedding = embedder.Embed(question.Text);
    var typedVector = new Vector(queryEmbedding);
    // Same SQL, different LIMIT: retain all eight ranks for independent checks and inspection.
    var (fullRanks, _) = await Search(typedVector, 8);
    var (topThree, _) = await Search(typedVector, 3);
    Check(fullRanks.Count == 8 && topThree.Count == 3, $"Result counts: {question.Text}");
    Check(topThree.Select(x => x.Id).SequenceEqual(fullRanks.Take(3).Select(x => x.Id)), "Top three match full ordering");
    Check(fullRanks.Select(x => x.Id).SequenceEqual(fullRanks.OrderBy(x => x.CosineDistance).ThenBy(x => x.Id).Select(x => x.Id)), "Distance/ID ordering");
    foreach (var hit in fullRanks)
    {
        var difference = Math.Abs(CosineDistance(queryEmbedding, stored[hit.Id]) - hit.CosineDistance);
        maxDifference = Math.Max(maxDifference, difference);
        Check(difference <= 1e-5, $"Independent distance tolerance: {question.Text}, passage {hit.Id}");
        Check(Math.Abs(hit.Similarity - (1 - hit.CosineDistance)) <= 1e-12, "Similarity equals 1 - distance");
    }
    if (question.ExpectedId is int expected)
    {
        bool passed = topThree[0].Id == expected;
        if (passed) top1Passed++;
        Check(passed, $"Expected passage {expected} first: {question.Text}; got {topThree[0].Id}");
    }

    var plan = new List<string>();
    await using (var explain = new NpgsqlCommand("EXPLAIN (ANALYZE, BUFFERS) " + sql, connection))
    {
        explain.Parameters.AddWithValue(typedVector);
        explain.Parameters.AddWithValue(3);
        await using var reader = await explain.ExecuteReaderAsync();
        while (await reader.ReadAsync()) plan.Add(reader.GetString(0));
    }

    var samples = new List<Sample>();
    for (int i = 0; i < 55; i++)
    {
        var endToEnd = Stopwatch.StartNew();
        var embeddingTimer = Stopwatch.StartNew();
        var vector = embedder.Embed(question.Text);
        embeddingTimer.Stop();
        var (hits, databaseMs) = await Search(new Vector(vector), 3);
        endToEnd.Stop();
        if (i >= 5) samples.Add(new(embeddingTimer.Elapsed.TotalMilliseconds, databaseMs, endToEnd.Elapsed.TotalMilliseconds));
        // Assertions/serialization are outside all measured boundaries.
        Check(hits.Select(x => x.Id).SequenceEqual(topThree.Select(x => x.Id)), "Measured result ranking stable");
    }
    allSamples.AddRange(samples);
    reports.Add(new(question.Text, question.ExpectedId, topThree, fullRanks, plan,
        Summarize(samples.Select(x => x.EmbeddingMs)), Summarize(samples.Select(x => x.DatabaseMs)),
        Summarize(samples.Select(x => x.EndToEndMs)), samples));
}
using var process = Process.GetCurrentProcess();
var report = new
{
    TimestampUtc = DateTimeOffset.UtcNow, Passed = failures.Count == 0, Failures = failures,
    OriginalMiniLmRegressionPassed = true, RegressionFixtureCount = regression.RootElement.GetProperty("TokenizerFixtures").GetInt32(),
    Model = spec.Id, spec.Revision, spec.Precision, ModelStartupMs = startup.Elapsed.TotalMilliseconds,
    embedder.ModelLoadMs, embedder.TokenizerLoadMs, embedder.ArtifactVerificationMs,
    StoredCount = stored.Count, Dimensions = 384, AllStoredEmbeddingsFinite = stored.Values.All(x => x.All(float.IsFinite)),
    SupportedQueryTop1Passed = top1Passed, SupportedQueryCount = 6, MaximumCosineDistanceDifference = maxDifference,
    UnrelatedConclusion = "Nearest-neighbour retrieval does not itself establish relevance. The Magic Flute question has no relevant passage but returns three neighbours. Similarity is not confidence.",
    PgvectorVersion = extensionVersion, PostgresVersion = postgresVersion, Indexes = indexes,
    ExactQuery = sql, QuerySha256 = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes("search.sql"))),
    CasesSha256 = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes("cases.json"))),
    Runtime = RuntimeInformation.FrameworkDescription, OS = RuntimeInformation.OSDescription,
    Architecture = RuntimeInformation.ProcessArchitecture.ToString(), ProcessPeakRssBytes = process.PeakWorkingSet64,
    ConnectionEstablishedOutsideMeasurements = true, DataSources = 1, Connections = 1, TopK = 3, WarmupsPerQuestion = 5, SamplesPerQuestion = 50,
    OverallEmbedding = Summarize(allSamples.Select(x => x.EmbeddingMs)),
    OverallDatabase = Summarize(allSamples.Select(x => x.DatabaseMs)),
    OverallEndToEnd = Summarize(allSamples.Select(x => x.EndToEndMs)), Questions = reports
};
File.WriteAllText("/results/retrieval.json", JsonSerializer.Serialize(report, json));
Console.WriteLine($"Passed={report.Passed}; top1={top1Passed}/6; rows={stored.Count}; max cosine error={maxDifference:G8}");
foreach (var failure in failures) Console.Error.WriteLine(failure);
return failures.Count == 0 ? 0 : 1;

// Independent scalar calculation, no Pgvector helpers. Double accumulators throughout.
static double CosineDistance(float[] a, float[] b)
{
    double dot = 0, normA = 0, normB = 0;
    for (int i = 0; i < a.Length; i++) { dot += (double)a[i] * b[i]; normA += (double)a[i] * a[i]; normB += (double)b[i] * b[i]; }
    return 1 - dot / Math.Sqrt(normA * normB);
}
static Stats Summarize(IEnumerable<double> samples)
{
    var sorted = samples.Order().ToArray();
    int n = sorted.Length;
    return new(n, n % 2 == 0 ? (sorted[n / 2 - 1] + sorted[n / 2]) / 2 : sorted[n / 2], sorted[(int)Math.Ceiling(n * 0.95) - 1]);
}
record Passage(int Id, string Text);
record Question(string Text, int? ExpectedId);
record Cases(Passage[] Passages, Question[] Questions);
record Hit(int Rank, int Id, string SourceText, double CosineDistance, double Similarity);
record IndexInfo(string Name, string Definition);
record Sample(double EmbeddingMs, double DatabaseMs, double EndToEndMs);
record Stats(int Count, double MedianMs, double P95Ms);
record QuestionReport(string Question, int? ExpectedId, List<Hit> ReturnedTopThree, List<Hit> FullRanking,
    List<string> ExplainAnalyzeBuffers, Stats Embedding, Stats Database, Stats EndToEnd, List<Sample> Samples);
