namespace Rowles.LeanLucene.Search;

/// <summary>
/// Returns the maximum score from any sub-query for a matching document,
/// plus an optional tie-breaking bonus from remaining sub-queries.
/// </summary>
public sealed class DisjunctionMaxQuery : Query
{
    private readonly List<Query> _disjuncts = [];

    /// <summary>
    /// Contribution of non-maximum clauses to the document score:
    /// <c>score = max + tieBreakerMultiplier * sum(rest)</c>.
    /// </summary>
    public float TieBreakerMultiplier { get; }

    public IReadOnlyList<Query> Disjuncts => _disjuncts;

    /// <inheritdoc/>
    /// <remarks>Returns the field of the first disjunct, or empty string if none added yet.</remarks>
    public override string Field => _disjuncts.Count > 0 ? _disjuncts[0].Field : string.Empty;

    public DisjunctionMaxQuery(float tieBreakerMultiplier = 0.0f)
    {
        TieBreakerMultiplier = tieBreakerMultiplier;
    }

    public DisjunctionMaxQuery Add(Query query)
    {
        ArgumentNullException.ThrowIfNull(query);
        _disjuncts.Add(query);
        return this;
    }
}
