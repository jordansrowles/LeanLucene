namespace Rowles.LeanLucene.Search.Scoring;

/// <summary>
/// Abstraction for collecting search results. Implementations can count,
/// aggregate, or collect in custom ways beyond the default TopN scoring.
/// </summary>
public interface ICollector
{
    /// <summary>Accepts a matching document with its score.</summary>
    void Collect(int docId, float score);

    /// <summary>Total number of matching documents seen so far.</summary>
    int TotalHits { get; }
}

/// <summary>
/// A simple count-only collector that tracks hit count without storing results.
/// Useful for count queries where actual documents are not needed.
/// </summary>
public sealed class CountCollector : ICollector
{
    public int TotalHits { get; private set; }

    public void Collect(int docId, float score) => TotalHits++;
}

/// <summary>
/// Wraps a <see cref="TopNCollector"/> struct as an <see cref="ICollector"/> for
/// use in extensible search pipelines.
/// </summary>
public sealed class TopNCollectorWrapper : ICollector
{
    private TopNCollector _inner;

    public TopNCollectorWrapper(int maxSize) => _inner = new TopNCollector(maxSize);

    public int TotalHits => _inner.TotalHits;

    public void Collect(int docId, float score) => _inner.Collect(docId, score);

    public TopDocs ToTopDocs() => _inner.ToTopDocs();
}
