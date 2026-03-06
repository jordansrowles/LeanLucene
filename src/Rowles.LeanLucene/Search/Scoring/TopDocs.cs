namespace Rowles.LeanLucene.Search.Scoring;

/// <summary>
/// Encapsulates the results of a search query.
/// </summary>
public sealed class TopDocs
{
    /// <summary>Gets the total number of documents that matched the query (may be larger than <see cref="ScoreDocs"/>).</summary>
    public int TotalHits { get; }

    /// <summary>Gets the array of scored documents in descending score order.</summary>
    public ScoreDoc[] ScoreDocs { get; }

    /// <summary>Initialises a new <see cref="TopDocs"/> with the given total hit count and scored results.</summary>
    /// <param name="totalHits">Total number of matching documents across the entire index.</param>
    /// <param name="scoreDocs">Scored documents, typically the top-N by relevance.</param>
    public TopDocs(int totalHits, ScoreDoc[] scoreDocs)
    {
        TotalHits = totalHits;
        ScoreDocs = scoreDocs;
    }

    /// <summary>Gets an empty <see cref="TopDocs"/> with zero hits.</summary>
    public static TopDocs Empty => new(0, []);
}
