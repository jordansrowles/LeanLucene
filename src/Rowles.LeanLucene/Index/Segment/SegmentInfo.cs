using System.Text.Json;

namespace Rowles.LeanLucene.Index.Segment;

/// <summary>
/// Metadata record for a single immutable segment.
/// </summary>
public sealed class SegmentInfo
{
    public string SegmentId { get; init; } = string.Empty;
    public int DocCount { get; init; }
    public int LiveDocCount { get; internal set; }
    public int CommitGeneration { get; init; }
    public List<string> FieldNames { get; init; } = [];

    /// <summary>
    /// Whether this segment's data files have been packed into a compound file (.cfs).
    /// When true, <see cref="SegmentReader"/> reads sub-files from the .cfs container.
    /// </summary>
    public bool IsCompoundFile { get; init; }

    /// <summary>
    /// Serialised index sort fields for this segment. Null if the segment is unsorted.
    /// Each entry is "Type:FieldName:Descending" (e.g. "Numeric:price:True").
    /// </summary>
    public List<string>? IndexSortFields { get; init; }

    public void WriteTo(string filePath)
    {
        var json = JsonSerializer.Serialize(this);
        File.WriteAllText(filePath, json);
    }

    public static SegmentInfo ReadFrom(string filePath)
    {
        var json = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<SegmentInfo>(json)
            ?? throw new InvalidDataException("Failed to deserialise segment info.");
    }
}
