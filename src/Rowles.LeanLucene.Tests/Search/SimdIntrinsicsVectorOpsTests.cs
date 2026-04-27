using Rowles.LeanLucene.Search;
using Rowles.LeanLucene.Search.Simd;
using Rowles.LeanLucene.Search.Parsing;
using Rowles.LeanLucene.Search.Highlighting;

namespace Rowles.LeanLucene.Tests.Search;

[Trait("Category", "Phase5")]
public sealed class SimdIntrinsicsVectorOpsTests
{
    [Fact]
    public void CosineSimilarity_MatchesNumericsImplementation()
    {
        var rnd = new Random(7);
        foreach (var dim in new[] { 8, 31, 64, 100, 257, 1024 })
        {
            var a = new float[dim];
            var b = new float[dim];
            for (int i = 0; i < dim; i++)
            {
                a[i] = (float)(rnd.NextDouble() * 2 - 1);
                b[i] = (float)(rnd.NextDouble() * 2 - 1);
            }

            float expected = SimdVectorOps.CosineSimilarity(a, b);
            float actual = SimdIntrinsicsVectorOps.CosineSimilarity(a, b);
            Assert.InRange(actual - expected, -1e-4f, 1e-4f);
        }
    }

    [Fact]
    public void DotProduct_MatchesNumericsImplementation()
    {
        var rnd = new Random(11);
        foreach (var dim in new[] { 8, 31, 64, 100, 257, 1024 })
        {
            var a = new float[dim];
            var b = new float[dim];
            for (int i = 0; i < dim; i++)
            {
                a[i] = (float)(rnd.NextDouble() * 2 - 1);
                b[i] = (float)(rnd.NextDouble() * 2 - 1);
            }
            float expected = SimdVectorOps.DotProduct(a, b);
            float actual = SimdIntrinsicsVectorOps.DotProduct(a, b);
            Assert.InRange(actual - expected, -1e-3f, 1e-3f);
        }
    }

    [Fact]
    public void CosineSimilarity_EmptyInputs_ReturnZero()
    {
        Assert.Equal(0f, SimdIntrinsicsVectorOps.CosineSimilarity([], []));
    }

    [Fact]
    public void CosineSimilarity_IdenticalVectors_ReturnOne()
    {
        float[] v = [1f, 2f, 3f, 4f, 5f, 6f, 7f, 8f];
        float sim = SimdIntrinsicsVectorOps.CosineSimilarity(v, v);
        Assert.InRange(sim, 0.999f, 1.001f);
    }
}
