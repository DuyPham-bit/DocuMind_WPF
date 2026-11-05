namespace DocuMind.Application.Interfaces;

public interface IEmbeddingService
{
    bool SupportsEmbeddings { get; }
    Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken ct = default);
}
