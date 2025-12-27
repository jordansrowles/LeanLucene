using System.Text.Json;

namespace Rowles.LeanLucene.Index;

/// <summary>
/// Metadata record for a single immutable segment.
/// </summary>
public sealed class SegmentInfo
{
    public string SegmentId { get; set; } = string.Empty;
    public int DocCount { get; set; }
    public int LiveDocCount { get; set; }
    public int CommitGeneration { get; set; }
    public List<string> FieldNames { get; set; } = [];

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
