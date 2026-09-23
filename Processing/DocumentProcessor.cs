using System.Text;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace Chatbot.Processing;

public sealed record PreparedChunk(string Heading, int? PageNumber, int Start, int End, string Content, int Tokens, float[] Embedding);
public sealed record PreparedDocument(string Text, IReadOnlyList<PreparedChunk> Chunks);

public sealed class DocumentProcessor
{
    private readonly Func<string, int> countTokens;
    private readonly Func<string, float[]> embed;
    public DocumentProcessor(OnnxEmbedder embedder) : this(text => embedder.Tokenize(text).Length - 2, embedder.Embed) { }
    internal DocumentProcessor(Func<string, int> countTokens, Func<string, float[]> embed)
    { this.countTokens = countTokens; this.embed = embed; }
    public const string Profile = "minilm-l6-v2-fp32-1110a243-mean-l2-v1";
    public PreparedDocument Prepare(byte[] original, string mediaType, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var pages = new List<(string Text, int? Page)>();
        if (mediaType == "text/plain") pages.Add((new UTF8Encoding(false, true).GetString(original).TrimStart('\uFEFF'), null));
        else if (mediaType == "application/pdf")
        {
            using var pdf = PdfDocument.Open(original);
            if (pdf.NumberOfPages > 100) throw new InvalidDataException("page_limit");
            int total = 0;
            foreach (var page in pdf.GetPages())
            {
                ct.ThrowIfCancellationRequested();
                string text = ContentOrderTextExtractor.GetText(page).Replace("\0", string.Empty);
                total += text.Length;
                if (total > 200_000) throw new InvalidDataException("text_limit");
                // Font-size headings are a heuristic; retain page boundaries even without headings.
                var sizes = page.Letters.Where(l => !string.IsNullOrWhiteSpace(l.Value)).Select(l => l.FontSize).Order().ToArray();
                double body = sizes.Length == 0 ? 0 : sizes[sizes.Length / 2];
                var headings = page.Letters.Where(l => l.FontSize >= body * 1.25 && body > 0)
                    .GroupBy(l => Math.Round(l.StartBaseLine.Y / 3))
                    .Select(g => string.Concat(g.OrderBy(l => l.StartBaseLine.X).Select(l => l.Value)).Trim())
                    .Where(s => s.Length is > 0 and <= 180).ToHashSet();
                text = string.Join('\n', text.Replace("\r\n", "\n").Split('\n').Select(line => headings.Contains(line.Trim()) ? "# " + line.Trim() : line))
                    .Replace("\0", string.Empty);
                pages.Add((text, page.Number));
            }
        }
        else throw new InvalidDataException("unsupported_type");
        string extracted = string.Join("\n\n", pages.Select(p => p.Text));
        if (extracted.Length > 200_000) throw new InvalidDataException("text_limit");
        if (string.IsNullOrWhiteSpace(extracted)) throw new InvalidDataException("no_text");
        var chunks = new List<PreparedChunk>();
        var windows = new TokenWindows(countTokens);
        int offset = 0;
        foreach (var (text, page) in pages)
        {
            // Horizontal whitespace only: an empty heading must not consume the next line.
            var headings = Regex.Matches(text, @"(?m)^#{1,6}[ \t]+[^\r\n]+", RegexOptions.None, TimeSpan.FromSeconds(1));
            var sections = new List<(int Start, int End, string Heading)>();
            int start = 0; string heading = "";
            foreach (Match match in headings)
            {
                if (match.Index > start) sections.Add((start, match.Index, heading));
                start = match.Index; heading = match.Value.TrimStart('#', ' ', '\t');
            }
            sections.Add((start, text.Length, heading));
            foreach (var section in sections)
                foreach (var span in windows.Split(text, section.Start, section.End, ct))
                {
                    ct.ThrowIfCancellationRequested();
                    if (chunks.Count >= 512) throw new InvalidDataException("chunk_limit");
                    string content = text[span.Start..span.End];
                    int tokens = windows.Count(content);
                    if (tokens == 0) continue;
                    chunks.Add(new(section.Heading, page, offset + span.Start, offset + span.End, content, tokens, embed(content)));
                }
            offset += text.Length + 2;
        }
        if (chunks.Count == 0) throw new InvalidDataException("no_text");
        return new(extracted, chunks);
    }
}
