using System.Text;
using System.Text.Json;
using DocuMind.Application.DTOs;
using DocuMind.Application.Interfaces;
using DocuMind.Domain.Entities;

namespace DocuMind.Application.Services;

public class RagOrchestratorService
{
    private readonly IChatProviderFactory _providerFactory;
    private readonly IDocumentParser _documentParser;
    private readonly IDocumentChunkRepository _chunkRepo;
    private readonly IRepository<Workspace> _workspaceRepo;

    public RagOrchestratorService(
        IChatProviderFactory providerFactory,
        IDocumentParser documentParser,
        IDocumentChunkRepository chunkRepo,
        IRepository<Workspace> workspaceRepo)
    {
        _providerFactory = providerFactory;
        _documentParser = documentParser;
        _chunkRepo = chunkRepo;
        _workspaceRepo = workspaceRepo;
    }

    /// <summary>
    /// Direct LLM Chat (Chat ra ngoài không dùng RAG)
    /// </summary>
    public async Task<string> ChatExternalAsync(string systemPrompt, string userQuestion, double temperature = 0.7, CancellationToken ct = default)
    {
        var aiService = await _providerFactory.GetActiveProviderAsync();
        return await aiService.GenerateResponseAsync(systemPrompt, userQuestion, temperature, ct);
    }

    /// <summary>
    /// Ingestion Pipeline: Parse file -> Sliding window chunking -> Vector embedding -> Store in SQLite
    /// </summary>
    public async Task IngestDocumentAsync(Guid workspaceId, string filePath, IProgress<string>? progress = null, CancellationToken ct = default)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("Tài liệu không tồn tại trên đĩa.", filePath);

        progress?.Report("Đang đọc nội dung tài liệu...");
        var pages = await _documentParser.ParseFileAsync(filePath, ct);
        if (pages.Count == 0)
            throw new InvalidOperationException("Không thể trích xuất văn bản từ tệp này (có thể là file scan hoặc rỗng).");

        var fileName = Path.GetFileName(filePath);
        var aiService = await _providerFactory.GetActiveProviderAsync();

        // Xóa các chunk cũ của file này nếu nạp lại
        await _chunkRepo.DeleteByFileNameAsync(workspaceId, fileName);

        var newChunks = new List<DocumentChunk>();
        int totalChunks = 0;

        foreach (var (pageNumber, pageText) in pages)
        {
            var textChunks = VectorMathHelper.ChunkText(pageText, maxWords: 180, overlapWords: 30);
            foreach (var chunkText in textChunks)
            {
                totalChunks++;
                progress?.Report($"Đang tạo vector embedding cho đoạn {totalChunks} (Trang {pageNumber})...");
                var vector = await aiService.GenerateEmbeddingAsync(chunkText, ct);

                newChunks.Add(new DocumentChunk
                {
                    WorkspaceId = workspaceId,
                    FileName = fileName,
                    PageNumber = pageNumber,
                    Content = chunkText,
                    EmbeddingJson = JsonSerializer.Serialize(vector),
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        progress?.Report($"Đang lưu {newChunks.Count} đoạn vào cơ sở dữ liệu...");
        await _chunkRepo.AddRangeAsync(newChunks);
        await _chunkRepo.SaveChangesAsync();
        progress?.Report($"Hoàn tất nạp tài liệu '{fileName}' ({newChunks.Count} đoạn).");
    }

    /// <summary>
    /// RAG Pipeline: Vector search -> Top-K context -> Augmented Prompt -> LLM response with citations
    /// </summary>
    public async Task<ChatResultDto> ChatWithRagAsync(
        Guid workspaceId,
        string systemPrompt,
        string userQuestion,
        int topK = 4,
        double temperature = 0.7,
        CancellationToken ct = default)
    {
        var allChunks = await _chunkRepo.GetByWorkspaceAsync(workspaceId);
        if (allChunks.Count == 0)
        {
            return new ChatResultDto(
                "Chưa có tài liệu nào trong Không gian làm việc này. Bạn hãy bấm nút 'Nạp tài liệu' (PDF/TXT/MD) trước khi hỏi đáp RAG.",
                new List<RagSourceDto>());
        }

        var aiService = await _providerFactory.GetActiveProviderAsync();
        var queryVector = await aiService.GenerateEmbeddingAsync(userQuestion, ct);

        // Cosine similarity retrieval
        var scoredChunks = allChunks
            .Select(c =>
            {
                float[]? embedding = null;
                try
                {
                    embedding = JsonSerializer.Deserialize<float[]>(c.EmbeddingJson);
                }
                catch { }

                float score = VectorMathHelper.CosineSimilarity(queryVector, embedding);
                return new
                {
                    Chunk = c,
                    Score = score
                };
            })
            .Where(x => x.Score > 0.05f)
            .OrderByDescending(x => x.Score)
            .Take(topK)
            .ToList();

        if (scoredChunks.Count == 0)
        {
            // Fallback lấy top chunks cao nhất nếu không filter được
            scoredChunks = allChunks
                .Select(c =>
                {
                    float[]? embedding = null;
                    try { embedding = JsonSerializer.Deserialize<float[]>(c.EmbeddingJson); } catch { }
                    return new { Chunk = c, Score = VectorMathHelper.CosineSimilarity(queryVector, embedding) };
                })
                .OrderByDescending(x => x.Score)
                .Take(topK)
                .ToList();
        }

        var contextBuilder = new StringBuilder();
        var sources = new List<RagSourceDto>();

        foreach (var item in scoredChunks)
        {
            contextBuilder.AppendLine($"[Nguồn: {item.Chunk.FileName} - Trang {item.Chunk.PageNumber} (Độ khớp: {item.Score:P0})]:");
            contextBuilder.AppendLine(item.Chunk.Content);
            contextBuilder.AppendLine("---");

            sources.Add(new RagSourceDto(
                item.Chunk.FileName,
                item.Chunk.PageNumber,
                item.Chunk.Content,
                item.Score
            ));
        }

        string augmentedPrompt = $"""
            Dựa vào ngữ cảnh tài liệu được cung cấp dưới đây để trả lời câu hỏi của người dùng một cách chính xác, trung thực.
            Bắt buộc trích dẫn rõ [Nguồn: Tên file - Trang số] ở cuối mỗi luận điểm lấy từ tài liệu.
            Nếu thông tin không có trong tài liệu, hãy nói rõ là tài liệu hiện tại không đề cập, không được tự suy đoán.

            NGỮ CẢNH TÀI LIỆU CUNG CẤP:
            {contextBuilder}

            CÂU HỎI CỦA NGƯỜI DÙNG:
            {userQuestion}
            """;

        string answer = await aiService.GenerateResponseAsync(systemPrompt, augmentedPrompt, temperature, ct);
        return new ChatResultDto(answer, sources);
    }

    /// <summary>
    /// Lấy danh sách tên file đã nạp trong workspace
    /// </summary>
    public async Task<List<string>> GetWorkspaceFilesAsync(Guid workspaceId)
    {
        return await _chunkRepo.GetDistinctFilesByWorkspaceAsync(workspaceId);
    }

    /// <summary>
    /// Xóa tài liệu khỏi workspace
    /// </summary>
    public async Task DeleteDocumentAsync(Guid workspaceId, string fileName)
    {
        await _chunkRepo.DeleteByFileNameAsync(workspaceId, fileName);
    }
}

