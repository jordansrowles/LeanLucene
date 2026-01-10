namespace Rowles.LeanLucene.Search;

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
}
