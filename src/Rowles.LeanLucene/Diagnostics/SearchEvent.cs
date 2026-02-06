namespace Rowles.LeanLucene.Diagnostics;

/// <summary>
/// A single search event captured by <see cref="SearchAnalytics"/>.
/// </summary>
public sealed record SearchEvent
{
    public DateTime Timestamp { get; init; }
    public string QueryType { get; init; } = string.Empty;
    public string Query { get; init; } = string.Empty;
    public double ElapsedMs { get; init; }
    public int TotalHits { get; init; }
    public bool CacheHit { get; init; }
}
