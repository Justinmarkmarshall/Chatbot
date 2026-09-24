using System.Text;
using Chatbot.Services;

namespace Chatbot.UnitTests;

public class UploadValidationTests
{
    [Theory]
    [InlineData(@"C:\upload\report.PDF", "report.PDF", "application/pdf")]
    [InlineData("../../folder/note.TxT", "note.TxT", "text/plain")]
    public void PathsAreRemovedAndExtensionsAreCaseInsensitive(string input, string name, string type) =>
        Assert.Equal((name, type), DocumentUploadValidation.FileName(input));

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("bad\n.txt")]
    [InlineData("bad.exe")]
    [InlineData("folder/")]
    public void InvalidNamesAreRejected(string name) => Assert.Throws<ArgumentException>(() => DocumentUploadValidation.FileName(name));

    [Fact]
    public void FilenameAndFileSizeBoundaries()
    {
        Assert.Equal(180, DocumentUploadValidation.FileName(new string('a', 176) + ".txt").Name.Length);
        Assert.Throws<ArgumentException>(() => DocumentUploadValidation.FileName(new string('a', 177) + ".txt"));
        Assert.False(DocumentUploadValidation.ExceedsSize(DocumentService.MaxBytes - 1, 1));
        Assert.True(DocumentUploadValidation.ExceedsSize(DocumentService.MaxBytes, 1));
    }

    [Theory]
    [InlineData("text/plain")]
    [InlineData("application/pdf")]
    public void EmptyFilesAreRejected(string type) => Assert.Throws<ArgumentException>(() => DocumentUploadValidation.Content([], type));

    [Fact]
    public void ContentValidationRejectsInvalidUtf8NullsAndPdfSignatures()
    {
        DocumentUploadValidation.Content(Encoding.UTF8.GetBytes("\uFEFFRésumé 😀"), "text/plain");
        DocumentUploadValidation.Content("%PDF-1.7"u8.ToArray(), "application/pdf");
        Assert.Throws<ArgumentException>(() => DocumentUploadValidation.Content([0xC3, 0x28], "text/plain"));
        Assert.Throws<ArgumentException>(() => DocumentUploadValidation.Content([65, 0, 66], "text/plain"));
        Assert.Throws<ArgumentException>(() => DocumentUploadValidation.Content("not PDF"u8.ToArray(), "application/pdf"));
    }
}
