namespace Rowles.LeanLucene.Search.Queries;

/// <summary>
/// Combines sub-queries with MUST, SHOULD, and MUST_NOT clauses.
/// </summary>
public sealed class BooleanQuery : Query
{
    private readonly List<BooleanClause> _clauses = [];

    /// <inheritdoc/>
    /// <remarks>Returns the field of the first clause, or empty string if no clauses have been added.</remarks>
    public override string Field => _clauses.Count > 0 ? _clauses[0].Query.Field : string.Empty;

    /// <summary>Gets the list of boolean clauses that compose this query.</summary>
    public IReadOnlyList<BooleanClause> Clauses => _clauses;

    /// <summary>Adds a sub-query with the specified occurrence type.</summary>
    /// <param name="query">The sub-query to add.</param>
    /// <param name="occur">How this clause participates in matching and scoring.</param>
    public void Add(Query query, Occur occur)
    {
        _clauses.Add(new BooleanClause(query, occur));
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj) =>
        obj is BooleanQuery other &&
        Boost == other.Boost &&
        _clauses.Count == other._clauses.Count &&
        _clauses.SequenceEqual(other._clauses);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var h = new HashCode();
        h.Add(nameof(BooleanQuery));
        foreach (var c in _clauses) h.Add(c);
        return CombineBoost(h.ToHashCode());
    }
}
