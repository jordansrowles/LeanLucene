namespace Rowles.LeanLucene.Search.Searcher;

/// <summary>
/// Configuration for the IndexSearcher.
/// </summary>
public sealed class IndexSearcherConfig
{
    /// <summary>Scoring model. Default: BM25.</summary>
    public ISimilarity Similarity { get; set; } = Bm25Similarity.Instance;

    /// <summary>
    /// Whether to use parallel segment search when multiple segments exist.
    /// Disable for deterministic ordering or low-latency single-segment workloads. Default: true.
    /// </summary>
    public bool ParallelSearch { get; set; } = true;

    /// <summary>
    /// Maximum degree of parallelism for multi-segment search.
    /// -1 means use Environment.ProcessorCount. Default: -1.
    /// </summary>
    public int MaxConcurrency { get; set; } = -1;
}
