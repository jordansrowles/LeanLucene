namespace Rowles.LeanLucene.Search.Aggregations;

/// <summary>
/// Describes a numeric aggregation to compute alongside a search query.
/// </summary>
public sealed class AggregationRequest
{
    public AggregationRequest(string name, string field, AggregationType type = AggregationType.Stats)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Field = field ?? throw new ArgumentNullException(nameof(field));
        Type = type;
    }

    /// <summary>Caller-defined label for this aggregation.</summary>
    public string Name { get; }

    /// <summary>Numeric doc-values field to aggregate over.</summary>
    public string Field { get; }

    /// <summary>The kind of aggregation to compute.</summary>
    public AggregationType Type { get; }

    /// <summary>Histogram bucket width (only used when <see cref="Type"/> is <see cref="AggregationType.Histogram"/>).</summary>
    public double HistogramInterval { get; init; } = 10.0;
}
