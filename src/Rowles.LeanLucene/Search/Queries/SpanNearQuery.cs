namespace Rowles.LeanLucene.Search.Queries;

/// <summary>
/// Matches documents where the spans from all clauses appear within <see cref="Slop"/> positions
/// of each other. When <see cref="InOrder"/> is true, the spans must appear in the given order.
/// </summary>
public sealed class SpanNearQuery : SpanQuery
{
    public IReadOnlyList<SpanQuery> Clauses { get; }
    public int Slop { get; }
    public bool InOrder { get; }

    public override string Field => Clauses.Count > 0 ? Clauses[0].Field : string.Empty;

    public SpanNearQuery(SpanQuery[] clauses, int slop, bool inOrder = true)
    {
        ArgumentNullException.ThrowIfNull(clauses);
        ArgumentOutOfRangeException.ThrowIfNegative(slop);
        Clauses = clauses;
        Slop = slop;
        InOrder = inOrder;
    }

    public override bool Equals(object? obj) =>
        obj is SpanNearQuery other &&
        Slop == other.Slop && InOrder == other.InOrder && Boost == other.Boost &&
        Clauses.Count == other.Clauses.Count &&
        Clauses.SequenceEqual(other.Clauses);

    public override int GetHashCode()
    {
        var h = new HashCode();
        h.Add(nameof(SpanNearQuery));
        h.Add(Slop);
        h.Add(InOrder);
        foreach (var c in Clauses) h.Add(c);
        return CombineBoost(h.ToHashCode());
    }
}
