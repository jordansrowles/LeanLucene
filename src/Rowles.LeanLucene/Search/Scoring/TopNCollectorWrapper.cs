namespace Rowles.LeanLucene.Search.Scoring;

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
