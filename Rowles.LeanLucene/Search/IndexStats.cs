namespace Rowles.LeanLucene.Search;

/// <summary>
/// Immutable corpus-wide statistics computed once at <see cref="IndexSearcher"/>
/// construction. Shared across all segment readers so BM25 scores are
/// comparable across segment boundaries.
/// </summary>
public sealed class IndexStats
{
    /// <summary>Total number of documents across all segments (including deleted).</summary>
    public int TotalDocCount { get; }

    /// <summary>Total number of live (non-deleted) documents across all segments.</summary>
    public int LiveDocCount { get; }

    /// <summary>Per-field average document length (in token count).</summary>
    private readonly Dictionary<string, float> _avgFieldLengths;

    /// <summary>Per-field total document frequency (number of docs containing the field).</summary>
    private readonly Dictionary<string, int> _fieldDocCounts;

    public IndexStats(
        int totalDocCount,
        int liveDocCount,
        Dictionary<string, float> avgFieldLengths,
        Dictionary<string, int> fieldDocCounts)
    {
        TotalDocCount = totalDocCount;
        LiveDocCount = liveDocCount;
        _avgFieldLengths = avgFieldLengths;
        _fieldDocCounts = fieldDocCounts;
    }

    /// <summary>Returns the average field length for a given field, defaulting to 1.0f.</summary>
    public float GetAvgFieldLength(string field)
        => _avgFieldLengths.GetValueOrDefault(field, 1.0f);

    /// <summary>Returns the number of documents containing the given field.</summary>
    public int GetFieldDocCount(string field)
        => _fieldDocCounts.GetValueOrDefault(field, 0);

    public static IndexStats Empty => new(0, 0, [], []);
}
