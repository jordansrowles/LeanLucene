namespace Rowles.LeanLucene.Search.Queries;

/// <summary>
/// Numeric range filter over NumericField values.
/// </summary>
public sealed class RangeQuery : Query
{
    public override string Field { get; }
    public double Min { get; }
    public double Max { get; }

    public RangeQuery(string field, double min, double max)
    {
        Field = field;
        Min = min;
        Max = max;
    }

    public override bool Equals(object? obj) =>
        obj is RangeQuery other &&
        string.Equals(Field, other.Field, StringComparison.Ordinal) &&
        Min == other.Min && Max == other.Max && Boost == other.Boost;

    public override int GetHashCode() => CombineBoost(HashCode.Combine(nameof(RangeQuery), Field, Min, Max));
}
