using Rowles.LeanLucene.Analysis;
using Rowles.LeanLucene.Search;

namespace Rowles.LeanLucene.Index.Indexer;

/// <summary>
/// Configuration for the IndexWriter.
/// </summary>
public sealed class IndexWriterConfig
{
    /// <summary>RAM buffer size in megabytes before an automatic flush.</summary>
    public double RamBufferSizeMB { get; set; } = 256.0;

    /// <summary>Maximum number of buffered documents before an automatic flush.</summary>
    public int MaxBufferedDocs { get; set; } = 10_000;

    /// <summary>
    /// Maximum number of documents that can be queued for indexing before AddDocument blocks.
    /// Provides backpressure to prevent unbounded memory growth. Set to 0 to disable (not recommended).
    /// Default: 2 × MaxBufferedDocs.
    /// </summary>
    public int MaxQueuedDocs { get; set; } = 20_000;

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

    /// <summary>
    /// Brotli compression level for stored fields. Default: <see cref="System.IO.Compression.CompressionLevel.Fastest"/>.
    /// Higher levels reduce disk size at the cost of slower writes; decompression speed is unaffected.
    /// </summary>
    public System.IO.Compression.CompressionLevel StoredFieldCompressionLevel { get; set; } = System.IO.Compression.CompressionLevel.Fastest;

    /// <summary>
    /// Number of documents per stored field block. Larger blocks compress better but
    /// increase random-access cost. Default: 16.
    /// </summary>
    public int StoredFieldBlockSize { get; set; } = 16;

    /// <summary>
    /// Skip interval for postings lists. Every N-th doc ID gets a skip pointer for O(log N)
    /// advance. Must be consistent between write and merge paths. Default: 128.
    /// </summary>
    public int PostingsSkipInterval { get; set; } = 128;

    /// <summary>
    /// Segment count threshold that triggers a tiered merge. When the number of segments
    /// at a given size tier reaches this value, the smallest are merged. Default: 10.
    /// </summary>
    public int MergeThreshold { get; set; } = 10;

    /// <summary>
    /// Maximum number of point values in a BKD tree leaf node. Smaller leaves give faster
    /// range queries at the cost of larger index files. Default: 512.
    /// </summary>
    public int BKDMaxLeafSize { get; set; } = 512;

    /// <summary>
    /// Maximum number of entries in the StandardAnalyser token intern cache.
    /// Larger caches reduce per-token string allocation for repeated terms. Default: 4096.
    /// </summary>
    public int AnalyserInternCacheSize { get; set; } = 4096;

    /// <summary>
    /// Custom stop words for the default StandardAnalyser. When null, the built-in English
    /// stop word list is used. Set to an empty list to disable stop word removal.
    /// </summary>
    public IReadOnlyList<string>? StopWords { get; set; }
}
