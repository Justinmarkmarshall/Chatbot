static class HeadingAwareChunker
{
    public static Chunk[] Run(SourceDocument doc, TokenWindows windows) =>
        windows.Materialize(doc, "heading-aware", doc.Sections.SelectMany(s => windows.Split(doc.Text, s.Start, s.End)));
}
