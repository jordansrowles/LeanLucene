namespace Rowles.LeanLucene.Search.Queries;

/// <summary>
/// Returns parent documents whose child documents match the given child query.
/// Follows the Lucene block-join pattern: children are stored immediately before
/// their parent within a segment.
/// </summary>
public sealed class BlockJoinQuery : Query
{
    /// <summary>Query that matches child documents.</summary>
    public Query ChildQuery { get; }

    /// <summary>
    /// Field name of the parent. When set, only parent docs whose stored field
    /// for the same field is non-empty are considered. This doubles as a signal
    /// that a parent bitset is expected during execution.
    /// </summary>
    public override string Field => ChildQuery.Field;

    public BlockJoinQuery(Query childQuery)
    {
        ChildQuery = childQuery ?? throw new ArgumentNullException(nameof(childQuery));
    }

    public override bool Equals(object? obj) =>
        obj is BlockJoinQuery other && ChildQuery.Equals(other.ChildQuery) && Boost == other.Boost;

    public override int GetHashCode() => CombineBoost(HashCode.Combine(nameof(BlockJoinQuery), ChildQuery));

    public override string ToString() => $"BlockJoinQuery({ChildQuery})^{Boost}";
}
