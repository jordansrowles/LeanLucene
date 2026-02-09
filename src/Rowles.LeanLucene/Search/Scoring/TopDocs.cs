namespace Rowles.LeanLucene.Search.Scoring;

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
