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

// Experimental implementation shared by source-linking only. No production dependency.
public sealed class OnnxEmbedder : IDisposable
{
    private readonly ModelSpec spec;
    private readonly WordPieceTokenizer tokenizer;
    private readonly InferenceSession session;
    public double ArtifactVerificationMs { get; }
    public double TokenizerLoadMs { get; }
    public double ModelLoadMs { get; }
    public IReadOnlyDictionary<string, NodeMetadata> InputMetadata => session.InputMetadata;
    public IReadOnlyDictionary<string, NodeMetadata> OutputMetadata => session.OutputMetadata;

    public OnnxEmbedder(ModelSpec spec, string modelRoot)
    {
        this.spec = spec;
        var directory = Path.Combine(modelRoot, spec.Id);
        var totalWatch = Stopwatch.StartNew();
        foreach (var (name, expected) in spec.Hashes)
        {
            using var file = File.OpenRead(Path.Combine(directory, name));
            var actual = Convert.ToHexStringLower(SHA256.HashData(file));
            if (actual != expected) throw new InvalidDataException($"Artifact hash mismatch: {name}");
        }
        ArtifactVerificationMs = totalWatch.Elapsed.TotalMilliseconds;
        var watch = Stopwatch.StartNew();
        using var vocab = File.OpenRead(Path.Combine(directory, "vocab.txt"));
        tokenizer = WordPieceTokenizer.Create(vocab, new WordPieceOptions
        {
            UnknownToken = "[UNK]",
            ContinuingSubwordPrefix = "##",
            MaxInputCharsPerWord = 100
        });
        TokenizerLoadMs = watch.Elapsed.TotalMilliseconds;
        using var tokenizerConfiguration = JsonDocument.Parse(File.ReadAllText(Path.Combine(directory, "tokenizer.json")));
        var configRoot = tokenizerConfiguration.RootElement;
        var normalizer = configRoot.GetProperty("normalizer");
        if (normalizer.GetProperty("type").GetString() != "BertNormalizer" || !normalizer.GetProperty("lowercase").GetBoolean()
            || !normalizer.GetProperty("clean_text").GetBoolean() || !normalizer.GetProperty("handle_chinese_chars").GetBoolean()
            || configRoot.GetProperty("model").GetProperty("type").GetString() != "WordPiece")
            throw new InvalidDataException("This spike supports only the pinned uncased BERT tokenizer profiles.");

        using var options = new SessionOptions
        {
            IntraOpNumThreads = 1,
            InterOpNumThreads = 1,
            ExecutionMode = ExecutionMode.ORT_SEQUENTIAL,
            GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL
        };
        options.AddSessionConfigEntry("session.intra_op.allow_spinning", "0");
        options.AddSessionConfigEntry("session.inter_op.allow_spinning", "0");
        watch.Restart();
        session = new InferenceSession(Path.Combine(directory, Path.GetFileName(spec.Graph)), options);
        ModelLoadMs = watch.Elapsed.TotalMilliseconds;



    }
    // Microsoft BertTokenizer 2.0.0 failed HF parity for tabs/newlines, astral symbols,
    // and added special tokens. Keep its WordPiece engine, with explicit Unicode-aware
    // BERT preprocessing verified against tokenizer.json reference fixtures.
    public long[] Tokenize(string text)
    {
        var result = new List<long> { 101 };
        var specials = new Dictionary<string, long> { ["[PAD]"] = 0, ["[UNK]"] = 100, ["[CLS]"] = 101, ["[SEP]"] = 102, ["[MASK]"] = 103 };
        foreach (var part in Regex.Split(text, @"(\[PAD\]|\[UNK\]|\[CLS\]|\[SEP\]|\[MASK\])"))
        {
            if (specials.TryGetValue(part, out var special)) { result.Add(special); continue; }
            var clean = new StringBuilder();
            foreach (var rune in part.EnumerateRunes())
            {
                var category = Rune.GetUnicodeCategory(rune);
                if (rune.Value == 0 || rune.Value == 0xfffd) continue;
                if (rune.Value is 9 or 10 or 13 || category == UnicodeCategory.SpaceSeparator) { clean.Append(' '); continue; }
                if (category is UnicodeCategory.Control or UnicodeCategory.Format) continue;
                bool cjk = rune.Value is >= 0x4e00 and <= 0x9fff or >= 0x3400 and <= 0x4dbf or >= 0x20000 and <= 0x2a6df
                    or >= 0x2a700 and <= 0x2b73f or >= 0x2b740 and <= 0x2b81f or >= 0x2b820 and <= 0x2ceaf
                    or >= 0xf900 and <= 0xfaff or >= 0x2f800 and <= 0x2fa1f;
                if (cjk) clean.Append(' ');
                clean.Append(rune.ToString());
                if (cjk) clean.Append(' ');
            }
            var word = new StringBuilder();
            void Flush()
            {
                if (word.Length == 0) return;
                result.AddRange(tokenizer.EncodeToIds(word.ToString(), considerPreTokenization: false, considerNormalization: false).Select(x => (long)x));
                word.Clear();
            }
            foreach (var rune in clean.ToString().ToLowerInvariant().Normalize(NormalizationForm.FormD).EnumerateRunes())
            {
                var category = Rune.GetUnicodeCategory(rune);
                if (category == UnicodeCategory.NonSpacingMark) continue;
                if (Rune.IsWhiteSpace(rune)) { Flush(); continue; }
                bool punctuation = rune.Value is >= 33 and <= 47 or >= 58 and <= 64 or >= 91 and <= 96 or >= 123 and <= 126
                    || category is UnicodeCategory.ConnectorPunctuation or UnicodeCategory.DashPunctuation
                    or UnicodeCategory.OpenPunctuation or UnicodeCategory.ClosePunctuation or UnicodeCategory.InitialQuotePunctuation
                    or UnicodeCategory.FinalQuotePunctuation or UnicodeCategory.OtherPunctuation;
                if (punctuation) Flush();
                word.Append(Rune.ToLowerInvariant(rune).ToString());
                if (punctuation) Flush();
            }
            Flush();
        }
        result.Add(102);
        return result.ToArray();
    }

    public float[][] Embed(string[] texts)
    {
        if (texts.Any(string.IsNullOrWhiteSpace)) throw new ArgumentException("Empty input");
        var encoded = texts.Select(Tokenize).ToArray();
        if (encoded.Any(x => x.Length > spec.MaxTokens)) throw new ArgumentException("Input exceeds model token limit");
        int length = encoded.Max(x => x.Length), batch = texts.Length;
        var ids = new DenseTensor<long>(new[] { batch, length });
        var masks = new DenseTensor<long>(new[] { batch, length });
        var types = new DenseTensor<long>(new[] { batch, length });
        for (int b = 0; b < batch; b++)
            for (int t = 0; t < encoded[b].Length; t++) { ids[b, t] = encoded[b][t]; masks[b, t] = 1; }
        var inputs = new List<NamedOnnxValue>();
        foreach (var name in session.InputMetadata.Keys)
            inputs.Add(NamedOnnxValue.CreateFromTensor(name, name switch
            {
                "input_ids" => ids,
                "attention_mask" => masks,
                "token_type_ids" => types,
                _ => throw new InvalidDataException($"Unexpected model input {name}")
            }));
        using var outputs = session.Run(inputs);
        var output = outputs.First();
        var metadata = session.OutputMetadata[output.Name];
        int[] dimensions;
        float[] values;
        if (metadata.ElementType == typeof(float))
        {
            var tensor = output.AsTensor<float>(); dimensions = tensor.Dimensions.ToArray(); values = tensor.ToArray();
        }
        else if (metadata.ElementType == typeof(Float16))
        {
            var tensor = output.AsTensor<Float16>(); dimensions = tensor.Dimensions.ToArray(); values = tensor.Select(x => (float)x).ToArray();
        }
        else throw new InvalidDataException($"Unexpected output type: {metadata.ElementType}");
        if (!dimensions.SequenceEqual(new[] { batch, length, 384 }))
            throw new InvalidDataException($"Unexpected output shape: {string.Join(',', dimensions)}");
        var vectors = new float[batch][];
        for (int b = 0; b < batch; b++)
        {
            var v = new float[384];
            for (int t = 0; t < encoded[b].Length; t++)
                for (int d = 0; d < 384; d++) v[d] += values[(b * length + t) * 384 + d] / encoded[b].Length;
            var norm = Math.Sqrt(v.Sum(x => (double)x * x));
            if (!double.IsFinite(norm) || norm == 0) throw new InvalidDataException("Nonfinite or zero embedding");
            for (int d = 0; d < 384; d++) v[d] = (float)(v[d] / norm);
            vectors[b] = v;
        }
        return vectors;
    }
    public float[] Embed(string text) => Embed(new[] { text })[0];
    public void Dispose() => session.Dispose();
}

public record ModelSpec(string Id, string Repository, string Revision, string Graph, int MaxTokens, string Precision,
    string QueryPrefix, string PassagePrefix, Dictionary<string, string> Hashes);
