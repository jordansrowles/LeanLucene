namespace Rowles.LeanLucene.Search.Queries;

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

    /// <summary>Gets the list of sub-queries whose scores are combined.</summary>
    public IReadOnlyList<Query> Disjuncts => _disjuncts;

    /// <inheritdoc/>
    /// <remarks>Returns the field of the first disjunct, or empty string if none added yet.</remarks>
    public override string Field => _disjuncts.Count > 0 ? _disjuncts[0].Field : string.Empty;

    /// <summary>Initialises a new <see cref="DisjunctionMaxQuery"/> with the given tie-breaker multiplier.</summary>
    /// <param name="tieBreakerMultiplier">
    /// Contribution factor for non-maximum matching clauses.
    /// Set to 0 to use only the maximum score; higher values add a fraction of remaining scores.
    /// </param>
    public DisjunctionMaxQuery(float tieBreakerMultiplier = 0.0f)
    {
        TieBreakerMultiplier = tieBreakerMultiplier;
    }

    /// <summary>Adds a disjunct sub-query and returns <c>this</c> for chaining.</summary>
    /// <param name="query">The sub-query to add as a disjunct.</param>
    /// <returns>This <see cref="DisjunctionMaxQuery"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="query"/> is <see langword="null"/>.</exception>
    public DisjunctionMaxQuery Add(Query query)
    {
        ArgumentNullException.ThrowIfNull(query);
        _disjuncts.Add(query);
        return this;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj) =>
        obj is DisjunctionMaxQuery other &&
        TieBreakerMultiplier == other.TieBreakerMultiplier &&
        Boost == other.Boost &&
        _disjuncts.Count == other._disjuncts.Count &&
        _disjuncts.SequenceEqual(other._disjuncts);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var h = new HashCode();
        h.Add(nameof(DisjunctionMaxQuery));
        h.Add(TieBreakerMultiplier);
        foreach (var d in _disjuncts) h.Add(d);
        return CombineBoost(h.ToHashCode());
    }
}
