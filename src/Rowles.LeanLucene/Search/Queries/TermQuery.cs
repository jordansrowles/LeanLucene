namespace Rowles.LeanLucene.Search.Queries;

/// <summary>
/// Single exact-term lookup against the index.
/// </summary>
public sealed class TermQuery : Query
{
    public override string Field { get; }
    public string Term { get; }

    /// <summary>Lazily-cached "field\0term" string used by IndexSearcher to avoid per-search allocation.</summary>
    internal string? CachedQualifiedTerm { get; set; }

    public TermQuery(string field, string term)
    {
        Field = field;
        Term = term;
    }

    public override bool Equals(object? obj) =>
        obj is TermQuery other &&
        string.Equals(Field, other.Field, StringComparison.Ordinal) &&
        string.Equals(Term, other.Term, StringComparison.Ordinal) &&
        Boost == other.Boost;

    public override int GetHashCode() => CombineBoost(HashCode.Combine(nameof(TermQuery), Field, Term));
}
