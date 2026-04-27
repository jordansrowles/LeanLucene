using Rowles.LeanLucene.Search;
using Rowles.LeanLucene.Search.Simd;
using Rowles.LeanLucene.Search.Parsing;
using Rowles.LeanLucene.Search.Highlighting;

namespace Rowles.LeanLucene.Tests.Search;

[Trait("Category", "Simd")]
public sealed class SimdVectorOpsTests
{
    [Fact]
    public void Cosine_OnIdenticalVectors_IsOne()
    {
        var v = new float[] { 1, 2, 3, 4, 5, 6, 7, 8 };

        float similarity = SimdVectorOps.CosineSimilarity(v, v);

        Assert.InRange(similarity, 0.9999f, 1.0001f);
    }

    [Fact]
    public void Cosine_OnOrthogonalVectors_IsZero()
    {
        var a = new float[] { 1, 0, 0, 0 };
        var b = new float[] { 0, 1, 0, 0 };

        float similarity = SimdVectorOps.CosineSimilarity(a, b);

        Assert.Equal(0f, similarity, 5);
    }

    [Fact]
    public void Cosine_OnEmptyOrMismatchedInputs_ReturnsZero()
    {
        Assert.Equal(0f, SimdVectorOps.CosineSimilarity(Array.Empty<float>(), Array.Empty<float>()));
        Assert.Equal(0f, SimdVectorOps.CosineSimilarity(new float[] { 1, 2 }, new float[] { 1, 2, 3 }));
    }

    [Fact]
    public void DotProduct_MatchesScalarReference()
    {
        var rng = new Random(17);
        var a = new float[37];
        var b = new float[37];
        for (int i = 0; i < a.Length; i++) { a[i] = (float)rng.NextDouble(); b[i] = (float)rng.NextDouble(); }
        float reference = 0f;
        for (int i = 0; i < a.Length; i++) reference += a[i] * b[i];

        float result = SimdVectorOps.DotProduct(a, b);

        Assert.Equal(reference, result, 4);
    }

    [Fact]
    public void NormaliseInPlace_ProducesUnitNorm()
    {
        var v = new float[] { 3, 4, 0, 0 };

        bool ok = SimdVectorOps.NormaliseInPlace(v);

        Assert.True(ok);
        Assert.Equal(1f, MathF.Sqrt(SimdVectorOps.SquaredNorm(v)), 5);
    }

    [Fact]
    public void NormaliseInPlace_OnZeroVector_ReturnsFalseAndLeavesInputUntouched()
    {
        var v = new float[] { 0, 0, 0, 0 };

        bool ok = SimdVectorOps.NormaliseInPlace(v);

        Assert.False(ok);
        Assert.All(v, x => Assert.Equal(0f, x));
    }

    [Fact]
    public void Normalise_OnZeroVector_Throws()
    {
        Assert.Throws<ArgumentException>(() => SimdVectorOps.Normalise(new float[] { 0, 0, 0 }));
    }

    [Fact]
    public void DotProduct_OfNormalised_EqualsCosineSimilarity()
    {
        var rng = new Random(99);
        var a = new float[64];
        var b = new float[64];
        for (int i = 0; i < a.Length; i++) { a[i] = (float)rng.NextDouble(); b[i] = (float)rng.NextDouble(); }

        var an = SimdVectorOps.Normalise(a);
        var bn = SimdVectorOps.Normalise(b);

        float dot = SimdVectorOps.DotProduct(an, bn);
        float cos = SimdVectorOps.CosineSimilarity(a, b);

        Assert.Equal(cos, dot, 4);
    }
}
