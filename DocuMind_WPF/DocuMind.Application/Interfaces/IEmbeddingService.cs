namespace DocuMind.Application.Interfaces;

public interface IEmbeddingService
{
    bool SupportsEmbeddings { get; }
    Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken ct = default);

    /// <summary>Số đoạn tối đa gửi trong một request khi nạp tài liệu. 1 nghĩa là provider không hỗ trợ gửi theo lô.</summary>
    int MaxBatchSize => 1;

    /// <summary>
    /// Tạo embedding cho nhiều đoạn, trả về đúng thứ tự đầu vào. Mặc định gọi từng đoạn;
    /// provider hỗ trợ lô (Gemini batchEmbedContents, OpenAI-compatible input mảng) ghi đè để giảm số request.
    /// </summary>
    async Task<IReadOnlyList<float[]>> GenerateEmbeddingsAsync(IReadOnlyList<string> texts, CancellationToken ct = default)
    {
        var vectors = new List<float[]>(texts.Count);
        foreach (var text in texts) vectors.Add(await GenerateEmbeddingAsync(text, ct));
        return vectors;
    }
}
