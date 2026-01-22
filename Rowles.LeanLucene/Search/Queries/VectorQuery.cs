using System.Numerics;
using System.Runtime.InteropServices;

namespace Rowles.LeanLucene.Search.Queries;

/// <summary>
/// ANN search over .vec data using a flat SIMD cosine similarity scan.
/// </summary>
public sealed class VectorQuery : Query
{
    public override string Field { get; }
    public float[] QueryVector { get; }
    public int TopK { get; }

    public VectorQuery(string field, float[] queryVector, int topK = 10)
    {
        Field = field;
        QueryVector = queryVector;
        TopK = topK;
    }

    /// <summary>
    /// Computes cosine similarity between two vectors using SIMD where available.
    /// </summary>
    public static float CosineSimilarity(ReadOnlySpan<float> a, ReadOnlySpan<float> b)
    {
        if (a.Length != b.Length || a.Length == 0)
            return 0f;

        float dot = 0f, normA = 0f, normB = 0f;

        int i = 0;

        if (Vector.IsHardwareAccelerated && a.Length >= Vector<float>.Count)
        {
            var vDot = Vector<float>.Zero;
            var vNormA = Vector<float>.Zero;
            var vNormB = Vector<float>.Zero;

            int simdEnd = a.Length - (a.Length % Vector<float>.Count);
            var spanA = MemoryMarshal.Cast<float, Vector<float>>(a[..simdEnd]);
            var spanB = MemoryMarshal.Cast<float, Vector<float>>(b[..simdEnd]);

            for (int j = 0; j < spanA.Length; j++)
            {
                vDot += spanA[j] * spanB[j];
                vNormA += spanA[j] * spanA[j];
                vNormB += spanB[j] * spanB[j];
            }

            dot = Vector.Dot(vDot, Vector<float>.One);
            normA = Vector.Dot(vNormA, Vector<float>.One);
            normB = Vector.Dot(vNormB, Vector<float>.One);
            i = simdEnd;
        }

        // Scalar remainder
        for (; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }

        float denom = MathF.Sqrt(normA) * MathF.Sqrt(normB);
        return denom > 0f ? dot / denom : 0f;
    }
}
