using System.Text;
using Chatbot.Processing;
using UglyToad.PdfPig.Writer;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;

namespace Chatbot.UnitTests;

public class DocumentProcessorTests
{
    private static DocumentProcessor Processor(Func<string, int>? count = null) => new(count ?? TokenWindowsTests.Words, _ => new float[384]);
    private static PreparedDocument Text(string text) => Processor().Prepare(Encoding.UTF8.GetBytes(text), "text/plain");

    [Fact]
    public void PreambleAdjacentHeadingsAndOffsetsArePreserved()
    {
        var doc = Text("\uFEFFPreamble\r\n# First\r\n## Second\r\nBody\n# Third\nLast");
        Assert.False(doc.Text.StartsWith('\uFEFF'));
        Assert.Equal(new[] { "", "First", "Second", "Third" }, doc.Chunks.Select(c => c.Heading));
        Assert.Equal("Preamble", doc.Chunks[0].Content);
        Assert.Equal("# First", doc.Chunks[1].Content);
        Assert.Contains("Body", doc.Chunks[2].Content);
        Assert.All(doc.Chunks, c => { Assert.Equal(c.Content, doc.Text[c.Start..c.End]); Assert.Null(c.PageNumber); });
    }

    [Fact]
    public void LongSectionsAreSubdividedAndEveryChunkIsEmbedded()
    {
        var embedded = new List<string>();
        var processor = new DocumentProcessor(TokenWindowsTests.Words, s => { embedded.Add(s); return [1, 2]; });
        var doc = processor.Prepare(Encoding.UTF8.GetBytes("# Heading\n" + string.Join(' ', Enumerable.Repeat("word", 600))), "text/plain");
        Assert.True(doc.Chunks.Count > 1);
        Assert.Equal(doc.Chunks.Select(c => c.Content), embedded);
        Assert.All(doc.Chunks, c => { Assert.Equal("Heading", c.Heading); Assert.InRange(c.Tokens, 1, 254); Assert.Equal(new float[] { 1, 2 }, c.Embedding); });
    }

    [Theory]
    [InlineData("# \n# Recovery\nRestore the latest backup.")]
    [InlineData("#\n# Recovery\nRestore the latest backup.")]
    [InlineData("# \r\n# Recovery\r\nRestore the latest backup.")]
    [InlineData("#\t\n# Recovery\nRestore the latest backup.")]
    public void EmptyHeadingDoesNotConsumeTheFollowingHeading(string text)
    {
        var doc = Text(text);
        Assert.Equal(2, doc.Chunks.Count);
        Assert.Equal("", doc.Chunks[0].Heading);
        Assert.Equal("#", doc.Chunks[0].Content);
        Assert.Equal("Recovery", doc.Chunks[1].Heading);
        Assert.Contains("Restore the latest backup.", doc.Chunks[1].Content);
        Assert.All(doc.Chunks, c => Assert.Equal(c.Content, doc.Text[c.Start..c.End]));
    }

    [Fact]
    public void EmptyHeadingDoesNotPromoteBodyTextAndTabsSeparateHeadings()
    {
        var doc = Text("#\nOrdinary body text\n##\tRecovery\nRestore backup.");
        Assert.Equal("", doc.Chunks[0].Heading);
        Assert.Contains("Ordinary body text", doc.Chunks[0].Content);
        Assert.Equal("Recovery", doc.Chunks[1].Heading);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" \r\n\t")]
    public void EmptyTextIsRejected(string text) => Assert.Equal("no_text", Assert.Throws<InvalidDataException>(() => Text(text)).Message);

    [Fact]
    public void InvalidTypesEncodingAndZeroTokensFail()
    {
        Assert.Equal("unsupported_type", Assert.Throws<InvalidDataException>(() => Processor().Prepare([1], "image/png")).Message);
        Assert.Throws<DecoderFallbackException>(() => Processor().Prepare([0xFF], "text/plain"));
        Assert.Equal("no_text", Assert.Throws<InvalidDataException>(() => Processor(_ => 0).Prepare([65], "text/plain")).Message);
    }

    [Fact]
    public void TextAndChunkLimitsAreInclusive()
    {
        Assert.Single(Processor(_ => 1).Prepare(Encoding.UTF8.GetBytes(new string('a', 200000)), "text/plain").Chunks);
        Assert.Equal("text_limit", Assert.Throws<InvalidDataException>(() => Text(new string('a', 200001))).Message);
        string Sections(int n) => string.Join('\n', Enumerable.Range(0, n).Select(i => $"# Section {i}\nBody"));
        Assert.Equal(512, Text(Sections(512)).Chunks.Count);
        Assert.Equal("chunk_limit", Assert.Throws<InvalidDataException>(() => Text(Sections(513))).Message);
    }

    // Small in-memory PDFs exercise extraction boundaries without ONNX or external fixtures.
    private static byte[] Pdf(int pages, bool blank = false)
    {
        var builder = new PdfDocumentBuilder();
        var font = builder.AddStandard14Font(Standard14Font.Helvetica);
        for (int i = 1; i <= pages; i++)
        {
            var page = builder.AddPage(PageSize.A4);
            if (blank) continue;
            page.AddText($"Heading {i}", 20, new PdfPoint(25, 750), font);
            page.AddText("This is the body text on this page with enough letters for a body font median.", 12, new PdfPoint(25, 710), font);
        }
        return builder.Build();
    }

    [Fact]
    public void PdfHeadingsPagesAndNormalizedOffsetsSurviveExtraction()
    {
        var doc = Processor().Prepare(Pdf(2), "application/pdf");
        Assert.Equal(new int?[] { 1, 2 }, doc.Chunks.Select(c => c.PageNumber).Distinct());
        Assert.Contains(doc.Chunks, c => c.Heading == "Heading 1");
        Assert.Contains(doc.Chunks, c => c.Heading == "Heading 2");
        Assert.DoesNotContain('\0', doc.Text);
        Assert.DoesNotContain("\r\n", doc.Text);
        Assert.All(doc.Chunks, c => Assert.Equal(c.Content, doc.Text[c.Start..c.End]));
    }

    [Fact]
    public void PdfPageLimitAndNoTextAreExplicit()
    {
        Assert.Equal(100, Processor().Prepare(Pdf(100), "application/pdf").Chunks.Count);
        Assert.Equal("page_limit", Assert.Throws<InvalidDataException>(() => Processor().Prepare(Pdf(101), "application/pdf")).Message);
        Assert.Equal("no_text", Assert.Throws<InvalidDataException>(() => Processor().Prepare(Pdf(1, true), "application/pdf")).Message);
    }

    [Fact]
    public void CancelledPreparationDoesNotEmbed()
    {
        var processor = new DocumentProcessor(_ => 1, _ => throw new Exception("Must not embed"));
        Assert.Throws<OperationCanceledException>(() => processor.Prepare([65], "text/plain", new CancellationToken(true)));
    }
}
