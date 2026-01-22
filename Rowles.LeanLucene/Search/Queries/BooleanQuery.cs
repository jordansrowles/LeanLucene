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
}

public sealed class BooleanClause
{
    public Query Query { get; }
    public Occur Occur { get; }

    public BooleanClause(Query query, Occur occur)
    {
        Query = query;
        Occur = occur;
    }
}
