namespace DocuMind.Application.Interfaces;

public interface IDocumentParser
{
    Task<List<(int PageNumber, string Text)>> ParseFileAsync(string filePath, CancellationToken ct = default);
}

