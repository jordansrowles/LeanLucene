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
