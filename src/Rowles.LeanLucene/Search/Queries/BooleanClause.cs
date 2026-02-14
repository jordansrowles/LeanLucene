namespace Rowles.LeanLucene.Search.Queries;

public sealed class BooleanClause : IEquatable<BooleanClause>
{
    public Query Query { get; }
    public Occur Occur { get; }

    public BooleanClause(Query query, Occur occur)
    {
        Query = query;
        Occur = occur;
    }

    public override bool Equals(object? obj) => obj is BooleanClause other && Equals(other);
    public bool Equals(BooleanClause? other) =>
        other is not null && Query.Equals(other.Query) && Occur == other.Occur;
    public override int GetHashCode() => HashCode.Combine(Query, Occur);
}
