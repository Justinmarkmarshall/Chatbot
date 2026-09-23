using System.Text;
namespace Chatbot.Services;

internal static class DocumentUploadValidation
{
    internal static bool ExceedsSize(long current, int incoming) => current + incoming > DocumentService.MaxBytes;
    internal static (string Name, string MediaType) FileName(string fileName)
    {
        fileName = fileName.Replace('\\', '/').Split('/').Last();
        if (string.IsNullOrWhiteSpace(fileName) || fileName.Length > 180 || fileName.Any(char.IsControl))
            throw new ArgumentException("Choose a filename of 1-180 characters without control characters.");
        string mediaType = Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".txt" => "text/plain",
            _ => throw new ArgumentException("Only PDF and UTF-8 .txt files are supported.")
        };
        return (fileName, mediaType);
    }
    internal static void Content(byte[] bytes, string mediaType)
    {
        if (bytes.Length == 0) throw new ArgumentException("Empty files cannot be uploaded.");
        if (mediaType == "application/pdf")
        {
            if (!bytes.AsSpan().StartsWith("%PDF-"u8)) throw new ArgumentException("The file does not have a PDF signature.");
        }
        else
        {
            try { _ = new UTF8Encoding(false, true).GetString(bytes); }
            catch (DecoderFallbackException) { throw new ArgumentException("Text documents must use valid UTF-8 encoding."); }
            if (bytes.Contains((byte)0)) throw new ArgumentException("Text documents must not contain null bytes.");
        }
    }
}
