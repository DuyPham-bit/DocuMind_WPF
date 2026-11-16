using DocuMind.Application.Services;
using FluentAssertions;
using Xunit;

namespace DocuMind.Tests;

public class VectorMathHelperTests
{
    [Fact]
    public void CosineSimilarity_IdenticalVectors_ReturnsOne()
    {
        float[] v1 = [1.0f, 2.0f, 3.0f];
        float[] v2 = [1.0f, 2.0f, 3.0f];

        float similarity = VectorMathHelper.CosineSimilarity(v1, v2);

        similarity.Should().BeApproximately(1.0f, 1e-4f);
    }

    [Fact]
    public void CosineSimilarity_OrthogonalVectors_ReturnsZero()
    {
        float[] v1 = [1.0f, 0.0f];
        float[] v2 = [0.0f, 1.0f];

        float similarity = VectorMathHelper.CosineSimilarity(v1, v2);

        similarity.Should().BeApproximately(0.0f, 1e-4f);
    }

    [Fact]
    public void CosineSimilarity_OppositeVectors_ReturnsMinusOne()
    {
        float[] v1 = [1.0f, 2.0f];
        float[] v2 = [-1.0f, -2.0f];

        float similarity = VectorMathHelper.CosineSimilarity(v1, v2);

        similarity.Should().BeApproximately(-1.0f, 1e-4f);
    }

    [Fact]
    public void CosineSimilarity_MismatchedDimensions_ReturnsZero()
    {
        float[] v1 = [1.0f, 2.0f, 3.0f];
        float[] v2 = [1.0f, 2.0f];

        float similarity = VectorMathHelper.CosineSimilarity(v1, v2);

        similarity.Should().Be(0.0f);
    }

    [Fact]
    public void CosineSimilarity_NullOrEmpty_ReturnsZero()
    {
        VectorMathHelper.CosineSimilarity(null, [1.0f]).Should().Be(0.0f);
        VectorMathHelper.CosineSimilarity([], []).Should().Be(0.0f);
    }

    [Fact]
    public void CosineSimilarity_ZeroMagnitude_ReturnsZero()
    {
        float[] v1 = [0.0f, 0.0f, 0.0f];
        float[] v2 = [1.0f, 2.0f, 3.0f];

        float similarity = VectorMathHelper.CosineSimilarity(v1, v2);

        similarity.Should().Be(0.0f);
    }
}

