namespace Rowles.LeanLucene.Search.Queries;

/// <summary>
/// Combines sub-queries with MUST, SHOULD, and MUST_NOT clauses.
/// </summary>
public sealed class BooleanQuery : Query
{
    private readonly List<BooleanClause> _clauses = [];

    public override string Field => _clauses.Count > 0 ? _clauses[0].Query.Field : string.Empty;

    public IReadOnlyList<BooleanClause> Clauses => _clauses;

    public void Add(Query query, Occur occur)
    {
        _clauses.Add(new BooleanClause(query, occur));
    }

    public override bool Equals(object? obj) =>
        obj is BooleanQuery other &&
        Boost == other.Boost &&
        _clauses.Count == other._clauses.Count &&
        _clauses.SequenceEqual(other._clauses);

    public override int GetHashCode()
    {
        var h = new HashCode();
        h.Add(nameof(BooleanQuery));
        foreach (var c in _clauses) h.Add(c);
        return CombineBoost(h.ToHashCode());
    }
}

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
