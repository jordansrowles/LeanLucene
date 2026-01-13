namespace Rowles.LeanLucene.Search;

/// <summary>
/// Pluggable scoring model. Allows replacing BM25 with alternative similarity functions.
/// </summary>
public interface ISimilarity
{
    /// <summary>Computes the score for a single term occurrence in a document.</summary>
    float Score(int termFreq, int docLength, float avgDocLength, int totalDocCount, int docFreq);

    /// <summary>Precomputes factors constant for a given term across all documents.</summary>
    (float Factor1, float Factor2) PrecomputeFactors(int totalDocCount, int docFreq, float avgDocLength);

    /// <summary>Scores using precomputed factors for hot-path scoring.</summary>
    float ScorePrecomputed(float factor1, float factor2, int termFreq, int docLength);
}

/// <summary>BM25 scoring (default). Delegates to <see cref="Bm25Scorer"/>.</summary>
public sealed class Bm25Similarity : ISimilarity
{
    public static readonly Bm25Similarity Instance = new();

    public float Score(int termFreq, int docLength, float avgDocLength, int totalDocCount, int docFreq)
        => Bm25Scorer.Score(termFreq, docLength, avgDocLength, totalDocCount, docFreq);

    public (float Factor1, float Factor2) PrecomputeFactors(int totalDocCount, int docFreq, float avgDocLength)
        => Bm25Scorer.PrecomputeFactors(totalDocCount, docFreq, avgDocLength);

    public float ScorePrecomputed(float factor1, float factor2, int termFreq, int docLength)
        => Bm25Scorer.ScorePrecomputed(factor1, factor2, termFreq, docLength);
}

/// <summary>Classic TF-IDF scoring model.</summary>
public sealed class TfIdfSimilarity : ISimilarity
{
    public static readonly TfIdfSimilarity Instance = new();

    public float Score(int termFreq, int docLength, float avgDocLength, int totalDocCount, int docFreq)
    {
        float tf = MathF.Sqrt(termFreq);
        float idf = 1.0f + MathF.Log((float)totalDocCount / (docFreq + 1));
        float lengthNorm = 1.0f / MathF.Sqrt(docLength);
        return tf * idf * lengthNorm;
    }

    public (float Factor1, float Factor2) PrecomputeFactors(int totalDocCount, int docFreq, float avgDocLength)
    {
        float idf = 1.0f + MathF.Log((float)totalDocCount / (docFreq + 1));
        return (idf, 0f);
    }

    public float ScorePrecomputed(float idf, float _, int termFreq, int docLength)
    {
        float tf = MathF.Sqrt(termFreq);
        float lengthNorm = 1.0f / MathF.Sqrt(docLength);
        return tf * idf * lengthNorm;
    }
}
