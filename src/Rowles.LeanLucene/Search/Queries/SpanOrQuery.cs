namespace Rowles.LeanLucene.Search.Queries;

/// <summary>Matches documents that match any of the provided span sub-queries.</summary>
public sealed class SpanOrQuery : SpanQuery
{
    public IReadOnlyList<SpanQuery> Clauses { get; }

    public override string Field => Clauses.Count > 0 ? Clauses[0].Field : string.Empty;

    public SpanOrQuery(params SpanQuery[] clauses)
    {
        ArgumentNullException.ThrowIfNull(clauses);
        Clauses = clauses;
    }

    public override bool Equals(object? obj) =>
        obj is SpanOrQuery other && Boost == other.Boost &&
        Clauses.Count == other.Clauses.Count &&
        Clauses.SequenceEqual(other.Clauses);

    public override int GetHashCode()
    {
        var h = new HashCode();
        h.Add(nameof(SpanOrQuery));
        foreach (var c in Clauses) h.Add(c);
        return CombineBoost(h.ToHashCode());
    }
}
