using Rowles.LeanLucene.Analysis;
using Rowles.LeanLucene.Search;

namespace Rowles.LeanLucene.Index;

/// <summary>
/// Configuration for the IndexWriter.
/// </summary>
public sealed class IndexWriterConfig
{
    /// <summary>RAM buffer size in megabytes before an automatic flush.</summary>
    public double RamBufferSizeMB { get; set; } = 256.0;

    /// <summary>Maximum number of buffered documents before an automatic flush.</summary>
    public int MaxBufferedDocs { get; set; } = 1000;

    /// <summary>
    /// Maximum number of documents that can be queued for indexing before AddDocument blocks.
    /// Provides backpressure to prevent unbounded memory growth. Set to 0 to disable (not recommended).
    /// Default: 2 × MaxBufferedDocs.
    /// </summary>
    public int MaxQueuedDocs { get; set; } = 2000;

    /// <summary>Default analyser used for fields without a specific mapping.</summary>
    public IAnalyser DefaultAnalyser { get; set; } = new StandardAnalyser();

    /// <summary>Per-field analyser overrides. Key is the field name.</summary>
    public Dictionary<string, IAnalyser> FieldAnalysers { get; set; } = new();

    /// <summary>Deletion policy applied after each commit. Default: keep latest only.</summary>
    public IIndexDeletionPolicy DeletionPolicy { get; set; } = new KeepLatestCommitPolicy();

    /// <summary>Scoring model used by IndexSearcher. Default: BM25.</summary>
    public ISimilarity Similarity { get; set; } = Bm25Similarity.Instance;

    /// <summary>Whether to store per-position payloads in the postings.</summary>
    public bool StorePayloads { get; set; }

    /// <summary>Whether to store term vectors for text fields.</summary>
    public bool StoreTermVectors { get; set; }

    /// <summary>Whether to write a compound file (.cfs) after segment flush.</summary>
    public bool UseCompoundFile { get; set; }
}
