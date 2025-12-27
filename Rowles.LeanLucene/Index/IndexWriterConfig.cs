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
}
