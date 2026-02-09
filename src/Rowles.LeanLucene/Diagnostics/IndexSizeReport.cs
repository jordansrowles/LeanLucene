namespace Rowles.LeanLucene.Diagnostics;

/// <summary>Overall index size report.</summary>
public sealed class IndexSizeReport
{
    public required string DirectoryPath { get; init; }
    public long TotalSizeBytes { get; init; }
    public long CommitFileSizeBytes { get; init; }
    public long StatsFileSizeBytes { get; init; }
    public IReadOnlyList<SegmentSizeReport> Segments { get; init; } = [];

    public long SegmentDataSizeBytes => Segments.Sum(s => s.TotalSizeBytes);
    public int SegmentCount => Segments.Count;

    /// <summary>Human-readable total size.</summary>
    public string TotalSizeFormatted => FormatBytes(TotalSizeBytes);

    private static string FormatBytes(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        < 1024 * 1024 * 1024 => $"{bytes / (1024.0 * 1024.0):F1} MB",
        _ => $"{bytes / (1024.0 * 1024.0 * 1024.0):F2} GB"
    };
}
