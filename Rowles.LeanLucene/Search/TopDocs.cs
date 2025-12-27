namespace Rowles.LeanLucene.Search;

/// <summary>
/// Represents a scored document in search results.
/// </summary>
public readonly record struct ScoreDoc(int DocId, float Score);

/// <summary>
/// Encapsulates the results of a search query.
/// </summary>
public sealed class TopDocs
{
    public int TotalHits { get; }
    public ScoreDoc[] ScoreDocs { get; }

    public TopDocs(int totalHits, ScoreDoc[] scoreDocs)
    {
        TotalHits = totalHits;
        ScoreDocs = scoreDocs;
    }

    public static TopDocs Empty => new(0, []);
}
