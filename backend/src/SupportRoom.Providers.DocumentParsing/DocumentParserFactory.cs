namespace SupportRoom.Providers.DocumentParsing;

/// <summary>Not GeneralException - this project doesn't (and shouldn't) depend on
/// SupportRoom.Application. The caller (IDocumentResourceService) catches this and wraps it as
/// GeneralException.ValidationError, same layering as every other Provider-to-Application boundary.</summary>
public sealed class UnsupportedDocumentTypeException(string contentType, string fileName)
    : Exception($"Unsupported document type \"{contentType}\" ({fileName}) - expected .pptx, .pdf, .docx, or .xlsx.");

public static class DocumentParserFactory
{
    public static IDocumentTextExtractor Create(string contentType, string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        if (contentType.Contains("presentation", StringComparison.OrdinalIgnoreCase) || extension == ".pptx")
        {
            return new PptxTextExtractor();
        }
        if (contentType.Contains("pdf", StringComparison.OrdinalIgnoreCase) || extension == ".pdf")
        {
            return new PdfTextExtractor();
        }
        if (contentType.Contains("wordprocessingml", StringComparison.OrdinalIgnoreCase) || extension == ".docx")
        {
            return new DocxTextExtractor();
        }
        if (contentType.Contains("spreadsheetml", StringComparison.OrdinalIgnoreCase) || extension == ".xlsx")
        {
            return new XlsxTextExtractor();
        }

        throw new UnsupportedDocumentTypeException(contentType, fileName);
    }
}
