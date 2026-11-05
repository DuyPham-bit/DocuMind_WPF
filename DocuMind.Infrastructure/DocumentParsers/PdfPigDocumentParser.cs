using DocuMind.Application.Interfaces;
using UglyToad.PdfPig;

namespace DocuMind.Infrastructure.DocumentParsers;

public class PdfPigDocumentParser : IDocumentParser
{
    public async Task<List<(int PageNumber, string Text)>> ParseFileAsync(string filePath, CancellationToken ct = default)
    {
        var result = new List<(int PageNumber, string Text)>();
        if (!File.Exists(filePath))
            return result;

        var ext = Path.GetExtension(filePath).ToLowerInvariant();

        if (ext == ".pdf")
        {
            await Task.Run(() =>
            {
                using var document = PdfDocument.Open(filePath);
                foreach (var page in document.GetPages())
                {
                    ct.ThrowIfCancellationRequested();
                    var text = page.Text?.Trim();
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        result.Add((page.Number, text));
                    }
                }
            }, ct);
        }
        else
        {
            // Hỗ trợ .txt, .md, .csv, .cs, .json,...
            var text = await File.ReadAllTextAsync(filePath, ct);
            if (!string.IsNullOrWhiteSpace(text))
            {
                result.Add((1, text.Trim()));
            }
        }

        return result;
    }
}
