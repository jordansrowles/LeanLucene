namespace Rowles.LeanLucene.Diagnostics;

internal sealed class SlowQueryEntry
{
    public DateTime Timestamp { get; init; }
    public string QueryType { get; init; } = string.Empty;
    public string Query { get; init; } = string.Empty;
    public double ElapsedMs { get; init; }
    public int TotalHits { get; init; }
}

[System.Text.Json.Serialization.JsonSerializable(typeof(SlowQueryEntry))]
internal sealed partial class SlowQueryJsonContext : System.Text.Json.Serialization.JsonSerializerContext;
