using DocuMind.Application.Services;
using FluentAssertions;
using Xunit;

namespace DocuMind.Tests;

public class ChunkingTests
{
    [Fact]
    public void ChunkText_NullOrEmpty_ReturnsEmptyList()
    {
        VectorMathHelper.ChunkText(null).Should().BeEmpty();
        VectorMathHelper.ChunkText("").Should().BeEmpty();
        VectorMathHelper.ChunkText("    \n\t").Should().BeEmpty();
    }

    [Fact]
    public void ChunkText_ShortText_ReturnsSingleChunk()
    {
        string text = "Đây là một đoạn văn bản ngắn để kiểm tra thuật toán cắt văn bản.";

        var chunks = VectorMathHelper.ChunkText(text, maxWords: 50, overlapWords: 10);

        chunks.Should().HaveCount(1);
        chunks[0].Should().Be(text);
    }

    [Fact]
    public void ChunkText_TextExceedingMaxWords_ReturnsMultipleOverlappingChunks()
    {
        // Tạo chuỗi có 25 từ: "w0 w1 w2 ... w24"
        var words = Enumerable.Range(0, 25).Select(i => $"word{i}").ToList();
        string text = string.Join(" ", words);

        // maxWords = 10, overlap = 3 -> step = 7
        // Chunk 0: word0..word9 (10 words)
        // Chunk 1: word7..word16 (10 words)
        // Chunk 2: word14..word23 (10 words)
        // Chunk 3: word21..word24 (4 words)
        var chunks = VectorMathHelper.ChunkText(text, maxWords: 10, overlapWords: 3);

        chunks.Should().HaveCount(4);
        chunks[0].Should().StartWith("word0").And.EndWith("word9");
        chunks[1].Should().StartWith("word7").And.EndWith("word16");
        chunks[2].Should().StartWith("word14").And.EndWith("word23");
        chunks[3].Should().StartWith("word21").And.EndWith("word24");
    }
}

