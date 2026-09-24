using System.Text;
using System.Text.RegularExpressions;
using Chatbot.Processing;

namespace Chatbot.UnitTests;

public class TokenWindowsTests
{
    internal static int Words(string s) => Regex.Matches(s, @"\S+").Count;

    [Theory]
    [InlineData(254, 1)]
    [InlineData(255, 2)]
    [InlineData(900, 5)]
    public void WordWindowsPreserveCoverageAndExactOverlap(int count, int expected)
    {
        string source = "prefix " + string.Join(' ', Enumerable.Range(0, count).Select(i => $"word{i}")) + " suffix";
        var windows = new TokenWindows(Words);
        var spans = windows.Split(source, 7, source.Length - 7).ToArray();
        Assert.Equal(expected, spans.Length);
        Assert.Equal(7, spans[0].Start);
        Assert.Equal(source.Length - 7, spans[^1].End);
        Assert.All(spans, s => Assert.InRange(Words(source[s.Start..s.End]), 1, 254));
        foreach (var (previous, next) in spans.Zip(spans.Skip(1)))
        {
            Assert.True(next.Start > previous.Start && next.End > previous.End);
            Assert.Equal(50, Words(source[next.Start..previous.End]));
        }
    }

    [Theory]
    [InlineData("a")]
    [InlineData("😀")]
    [InlineData("漢")]
    [InlineData("e\u0301")]
    public void OversizedUnbrokenTextPreservesUnicodeAndAllContent(string unit)
    {
        string source = string.Concat(Enumerable.Repeat(unit, 600));
        int Count(string s)
        {
            Assert.DoesNotContain('\uFFFD', s.EnumerateRunes().Select(r => (char)r.Value));
            return s.EnumerateRunes().Count();
        }
        var spans = new TokenWindows(Count).Split(source, 0, source.Length).ToArray();
        Assert.Equal(0, spans[0].Start);
        Assert.Equal(source.Length, spans[^1].End);
        Assert.All(spans, s => Assert.InRange(Count(source[s.Start..s.End]), 1, 254));
        foreach (var (a, b) in spans.Zip(spans.Skip(1)))
        {
            Assert.True(b.Start > a.Start && b.Start < a.End);
            Assert.Equal(50, Count(source[b.Start..a.End]));
        }
    }

    [Fact]
    public void WhitespaceIsTrimmedAndEmptyInputHasNoChunks()
    {
        var windows = new TokenWindows(Words);
        Assert.Empty(windows.Split(" \r\n\t", 0, 4));
        var span = Assert.Single(windows.Split(" \talpha beta\r\n", 0, 14));
        Assert.Equal("alpha beta", " \talpha beta\r\n"[span.Start..span.End]);
    }

    [Fact]
    public void ImpossibleProgressAndOverlapFailExplicitly()
    {
        Assert.Throws<InvalidDataException>(() => new TokenWindows(_ => 300).Split("abc", 0, 3).ToArray());
        Assert.Throws<InvalidDataException>(() => new TokenWindows(s => s.Length * 3).Split(new string('a', 300), 0, 300).ToArray());
    }

    [Fact]
    public void CancellationWorksForShortInputAndLongWordFallback()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        Assert.Throws<OperationCanceledException>(() => new TokenWindows(Words).Split("a", 0, 1, cts.Token).ToArray());
        using var during = new CancellationTokenSource();
        int calls = 0;
        var windows = new TokenWindows(s => { if (++calls == 5) during.Cancel(); return s.Length; });
        Assert.Throws<OperationCanceledException>(() => windows.Split(new string('a', 600), 0, 600, during.Token).ToArray());
        Assert.InRange(calls, 5, 6);
    }
}
