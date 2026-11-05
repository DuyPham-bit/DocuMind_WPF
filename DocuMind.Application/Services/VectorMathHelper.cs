namespace DocuMind.Application.Services;

public static class VectorMathHelper
{
    /// <summary>
    /// Computes the Cosine Similarity between two floating-point vectors in pure C#.
    /// Range: -1.0f to 1.0f. Returns 0f if lengths mismatch or either vector has 0 magnitude.
    /// </summary>
    public static float CosineSimilarity(float[]? v1, float[]? v2)
    {
        if (v1 == null || v2 == null || v1.Length == 0 || v1.Length != v2.Length)
            return 0f;

        float dot = 0f;
        float mag1 = 0f;
        float mag2 = 0f;

        for (int i = 0; i < v1.Length; i++)
        {
            dot += v1[i] * v2[i];
            mag1 += v1[i] * v1[i];
            mag2 += v2[i] * v2[i];
        }

        float denominator = MathF.Sqrt(mag1) * MathF.Sqrt(mag2);
        if (denominator <= 1e-8f)
            return 0f;

        return dot / denominator;
    }

    /// <summary>
    /// Splits text into overlapping chunks using a sliding window based on word count.
    /// </summary>
    public static List<string> ChunkText(string? text, int maxWords = 180, int overlapWords = 30)
    {
        var result = new List<string>();
        if (string.IsNullOrWhiteSpace(text))
            return result;

        if (maxWords <= 0) maxWords = 180;
        if (overlapWords < 0 || overlapWords >= maxWords) overlapWords = 0;

        var words = text.Split(new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0)
            return result;

        int step = maxWords - overlapWords;
        for (int i = 0; i < words.Length; i += step)
        {
            var slice = words.Skip(i).Take(maxWords).ToArray();
            if (slice.Length > 0)
            {
                result.Add(string.Join(" ", slice));
            }
            if (i + maxWords >= words.Length)
                break;
        }

        return result;
    }
}

