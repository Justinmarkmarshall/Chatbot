static class FixedTokenChunker
{
    public static Chunk[] Run(SourceDocument doc, TokenWindows windows) =>
        windows.Materialize(doc, "fixed-254-50", windows.Split(doc.Text, 0, doc.Text.Length));
}
