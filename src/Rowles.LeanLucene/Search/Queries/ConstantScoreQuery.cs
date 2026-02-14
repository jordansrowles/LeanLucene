namespace Rowles.LeanLucene.Search.Queries;

/// <summary>
/// Wraps an inner query and assigns a constant score to every matching document,
/// bypassing BM25 scoring. Useful for filter-mode boolean clauses.
/// </summary>
public sealed class ConstantScoreQuery : Query
{
    public Query Inner { get; }
    public float ConstantScore { get; }

    /// <inheritdoc/>
    public override string Field => Inner.Field;

    public ConstantScoreQuery(Query inner, float score = 1.0f)
    {
        ArgumentNullException.ThrowIfNull(inner);
        Inner = inner;
        ConstantScore = score;
    }

    public override bool Equals(object? obj) =>
        obj is ConstantScoreQuery other &&
        Inner.Equals(other.Inner) &&
        ConstantScore == other.ConstantScore &&
        Boost == other.Boost;

    public override int GetHashCode() => CombineBoost(HashCode.Combine(nameof(ConstantScoreQuery), Inner, ConstantScore));
}
