using System.Text.RegularExpressions;

static class ParagraphChunker
{
    public static Chunk[] Run(SourceDocument doc, TokenWindows windows)
    {
        var blocks = new List<Span>();
        int start = 0;
        foreach (Match separator in Regex.Matches(doc.Text, @"\r?\n[ \t]*\r?\n"))
        {
            Add(start, separator.Index);
            start = separator.Index + separator.Length;
        }
        Add(start, doc.Text.Length);
        var combined = new List<Span>();
        for (int i = 0; i < blocks.Count; i++)
        {
            var block = blocks[i];
            // Attach a standalone Markdown heading to its following block. Lists remain
            // one blank-line-delimited block. No unrelated short prose is merged.
            if (Regex.IsMatch(doc.Text[block.Start..block.End], @"\A#{1,6} [^\r\n]+\z") && i + 1 < blocks.Count)
                block = new(block.Start, blocks[++i].End);
            combined.Add(block);
        }
        return windows.Materialize(doc, "paragraph", combined.SelectMany(s => windows.Split(doc.Text, s.Start, s.End)));

        void Add(int a, int b)
        {
            while (a < b && char.IsWhiteSpace(doc.Text[a])) a++;
            while (b > a && char.IsWhiteSpace(doc.Text[b - 1])) b--;
            if (a < b) blocks.Add(new(a, b));
        }
    }
}
