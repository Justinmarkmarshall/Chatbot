using System.Text.RegularExpressions;

namespace Chatbot.Processing;

// Source spans are UTF-16 offsets in the unmodified UTF-8-decoded Markdown.
record Span(int Start, int End);
sealed class TokenWindows(Func<string, int> countTokens)
{
    public TokenWindows(OnnxEmbedder embedder) : this(text => embedder.Tokenize(text).Length - 2) { }
    public const int Maximum = 254;
    public const int Overlap = 50;
    public int Count(string text) => countTokens(text);

    public IEnumerable<Span> Split(string source, int start, int end, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        while (start < end && char.IsWhiteSpace(source[start])) start++;
        while (end > start && char.IsWhiteSpace(source[end - 1])) end--;
        while (start < end)
        {
            if (Count(source[start..end]) <= Maximum) { yield return new(start, end); yield break; }
            // Prefer complete whitespace-delimited words. Size is measured by the actual
            // MiniLM tokenizer, not words or characters; a window may be below the cap.
            int finish = start;
            foreach (Match word in Regex.Matches(source[start..end], @"\S+"))
            {
                ct.ThrowIfCancellationRequested();
                int candidate = start + word.Index + word.Length;
                if (Count(source[start..candidate]) > Maximum) break;
                finish = candidate;
            }
            if (finish == start)
            {
                // Pathological long first word: retain source rather than truncate it.
                for (int candidate = start + 1; candidate <= end; candidate++)
                {
                    ct.ThrowIfCancellationRequested();
                    if (candidate < end && char.IsLowSurrogate(source[candidate])) continue;
                    if (Count(source[start..candidate]) > Maximum) break;
                    finish = candidate;
                }
            }
            if (finish <= start || Count(source[start..finish]) <= Overlap)
                throw new InvalidDataException("Cannot make progress with configured token window.");
            yield return new(start, finish);

            // Exactly 50 content tokens when the overlap substring is tokenized alone.
            // Prefer word boundaries; if WordPiece skips that count, allow a character
            // boundary. Retokenize every complete next window, so no model truncation occurs.
            int next = -1;
            for (int i = finish - 1; i > start; i--)
            {
                ct.ThrowIfCancellationRequested();
                if (!char.IsWhiteSpace(source[i]) && char.IsWhiteSpace(source[i - 1]) && Count(source[i..finish]) == Overlap)
                { next = i; break; }
            }
            if (next < 0)
                for (int i = finish - 1; i > start; i--)
                {
                    ct.ThrowIfCancellationRequested();
                    if (!char.IsWhiteSpace(source[i]) && !char.IsLowSurrogate(source[i]) && Count(source[i..finish]) == Overlap)
                    { next = i; break; }
                }
            if (next <= start) throw new InvalidDataException("No exact 50-token overlap boundary; do not silently change overlap.");
            start = next;
        }
    }

}
