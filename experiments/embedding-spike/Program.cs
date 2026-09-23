using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text;
using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Microsoft.ML.Tokenizers;

// Deliberately a console experiment, not a production embedding service.
var json = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, WriteIndented = true };
var id = args.ElementAtOrDefault(0) ?? "minilm";
var modelRoot = args.ElementAtOrDefault(1) ?? "models";
var outputPath = args.ElementAtOrDefault(2) ?? $"results/{id}.json";
var specs = JsonSerializer.Deserialize<ModelSpec[]>(File.ReadAllText("models.json"), json)!;
var spec = specs.Single(x => x.Id == id);
var directory = Path.Combine(modelRoot, id);
var failures = new List<string>();
void Check(bool condition, string message) { if (!condition) failures.Add(message); }
long Rss() { using var p = Process.GetCurrentProcess(); return p.WorkingSet64; }
var baselineRss = Rss();
var totalWatch = Stopwatch.StartNew();

using var embedder = new OnnxEmbedder(spec, modelRoot);
var verificationMs = embedder.ArtifactVerificationMs;
var tokenizerLoadMs = embedder.TokenizerLoadMs;
var modelLoadMs = embedder.ModelLoadMs;
var loadedRss = Rss();
var watch = new Stopwatch();
long[] Tokenize(string text) => embedder.Tokenize(text);
float[][] Embed(string[] texts) => embedder.Embed(texts);
var fixture = JsonSerializer.Deserialize<FixtureSet>(File.ReadAllText($"fixtures/{id}.json"), json)!;
Check(fixture.Revision == spec.Revision, "Fixture revision does not match model");
foreach (var item in fixture.Cases)
{
    var ids = Tokenize(item.Text);
    Check(ids.SequenceEqual(item.Ids), $"Tokenizer parity: {JsonSerializer.Serialize(item.Text)} expected=[{string.Join(',', item.Ids)}] actual=[{string.Join(',', ids)}]");
    Check(Enumerable.Repeat(1L, ids.Length).SequenceEqual(item.AttentionMask), "Unpadded attention mask parity");
    Check(Enumerable.Repeat(0L, ids.Length).SequenceEqual(item.TypeIds), "Token type parity");
    Check((ids.Length > spec.MaxTokens) == item.ExceedsLimit, "Token limit boundary parity");
}
foreach (var item in fixture.Padded)
{
    var ids = Tokenize(item.Text);
    var padded = ids.Concat(Enumerable.Repeat(0L, 32 - ids.Length)).ToArray();
    var mask = Enumerable.Repeat(1L, ids.Length).Concat(Enumerable.Repeat(0L, 32 - ids.Length));
    Check(padded.SequenceEqual(item.Ids) && mask.SequenceEqual(item.AttentionMask), "Padded token/mask parity");
}

static double Cosine(float[] a, float[] b) => a.Zip(b).Sum(x => (double)x.First * x.Second) /
    Math.Sqrt(a.Sum(x => (double)x * x) * b.Sum(x => (double)x * x));

var cases = JsonSerializer.Deserialize<Cases>(File.ReadAllText("cases.json"), json)!;
watch.Restart();
var first = Embed([spec.QueryPrefix + cases.Groups[0].Query])[0];
var firstInferenceMs = watch.Elapsed.TotalMilliseconds;
var similarity = new List<object>();
foreach (var group in cases.Groups)
{
    var texts = new[] { spec.QueryPrefix + group.Query, spec.PassagePrefix + group.Related, spec.PassagePrefix + group.Unrelated };
    var vectors = Embed(texts);
    double related = Cosine(vectors[0], vectors[1]), unrelated = Cosine(vectors[0], vectors[2]);
    Check(related - unrelated >= cases.MinimumMargin, $"Similarity margin: {group.Name}");
    foreach (var v in vectors)
        Check(v.Length == 384 && v.All(float.IsFinite) && Math.Abs(v.Sum(x => (double)x * x) - 1) < 1e-5, "Embedding dimension/norm");
    var single = Embed([texts[0]])[0];
    Check(Cosine(single, vectors[0]) > 0.99999, $"Batch/padding equivalence: {group.Name}");
    similarity.Add(new { group.Name, Related = related, Unrelated = unrelated, Margin = related - unrelated });
}
Check(Cosine(first, Embed([spec.QueryPrefix + cases.Groups[0].Query])[0]) > 0.99999, "Repeatability");
foreach (var invalid in new[] { "", string.Concat(Enumerable.Repeat("word ", spec.MaxTokens)) })
{
    bool rejected = false;
    try { Embed([invalid]); } catch (ArgumentException) { rejected = true; }
    Check(rejected, "Invalid/oversized input rejection");
}

// Separate warm workloads; no download, initialization or correctness checks inside timings.
var benchmarks = new List<object>();
foreach (int words in new[] { 12, 200 })
{
    var text = spec.QueryPrefix + string.Join(' ', Enumerable.Repeat("document", words));
    for (int i = 0; i < 5; i++) Embed([text]);
    var times = new List<double>();
    for (int i = 0; i < 50; i++)
    {
        watch.Restart(); Embed([text]); times.Add(watch.Elapsed.TotalMilliseconds);
    }
    var sorted = times.Order().ToArray();
    benchmarks.Add(new
    {
        Words = words,
        Tokens = Tokenize(text).Length,
        Batch = 1,
        Iterations = 50,
        MedianMs = (sorted[24] + sorted[25]) / 2,
        P95Ms = sorted[47],
        MinMs = sorted[0],
        MaxMs = sorted[^1],
        SamplesMs = times
    });
}
using var process = Process.GetCurrentProcess();
var report = new
{
    TimestampUtc = DateTimeOffset.UtcNow,
    Model = spec.Id,
    spec.Repository,
    spec.Revision,
    spec.Precision,
    Runtime = RuntimeInformation.FrameworkDescription,
    OS = RuntimeInformation.OSDescription,
    Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
    VisibleProcessors = Environment.ProcessorCount,
    OnnxRuntime = OrtEnv.Instance().GetVersionString(),
    Threads = 1,
    TokenizerImplementation = "Microsoft.ML.Tokenizers WordPiece + explicit BERT preprocessing",
    ArtifactVerificationMs = verificationMs,
    TokenizerLoadMs = tokenizerLoadMs,
    ModelLoadMs = modelLoadMs,
    FirstInferenceMs = firstInferenceMs,
    BaselineRssBytes = baselineRss,
    LoadedRssBytes = loadedRss,
    FinalRssBytes = Rss(),
    PeakRssBytes = process.PeakWorkingSet64,
    TokenizerFixtures = fixture.Cases.Length + fixture.Padded.Length,
    Dimensions = first.Length,
    Similarity = similarity,
    Benchmarks = benchmarks,
    Passed = failures.Count == 0,
    Failures = failures,
    Inputs = embedder.InputMetadata.ToDictionary(x => x.Key, x => x.Value.ElementType.Name),
    Outputs = embedder.OutputMetadata.ToDictionary(x => x.Key, x => x.Value.ElementType.Name)
};
Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPath))!);
File.WriteAllText(outputPath, JsonSerializer.Serialize(report, json));
Console.WriteLine(JsonSerializer.Serialize(report, json));
return failures.Count == 0 ? 0 : 1;

record Fixture(string Text, long[] Ids, long[] AttentionMask, long[] TypeIds, bool ExceedsLimit);
record FixtureSet(string Revision, Fixture[] Cases, Fixture[] Padded);
record Group(string Name, string Query, string Related, string Unrelated);
record Cases(double MinimumMargin, Group[] Groups);

