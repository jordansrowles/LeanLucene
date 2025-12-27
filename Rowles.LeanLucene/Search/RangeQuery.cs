namespace Rowles.LeanLucene.Search;

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
}
