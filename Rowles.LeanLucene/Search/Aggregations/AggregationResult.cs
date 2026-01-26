namespace Rowles.LeanLucene.Search.Aggregations;

/// <summary>
/// Result of a numeric aggregation over matching documents.
/// </summary>
public sealed class AggregationResult
{
    public required string Name { get; init; }
    public required string Field { get; init; }
    public long Count { get; init; }
    public double Min { get; init; } = double.PositiveInfinity;
    public double Max { get; init; } = double.NegativeInfinity;
    public double Sum { get; init; }
    public double Avg => Count > 0 ? Sum / Count : 0.0;

    /// <summary>Histogram buckets (non-null only for Histogram aggregations).</summary>
    public IReadOnlyList<HistogramBucket>? Buckets { get; init; }

    /// <summary>An empty/no-data result.</summary>
    public static AggregationResult Empty(string name, string field)
        => new() { Name = name, Field = field, Count = 0, Min = 0, Max = 0, Sum = 0 };
}

/// <summary>A single histogram bucket.</summary>
public sealed record HistogramBucket(double LowerBound, double UpperBound, long Count);
