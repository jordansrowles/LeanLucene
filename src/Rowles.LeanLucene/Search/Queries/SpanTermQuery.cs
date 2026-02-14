namespace Rowles.LeanLucene.Search.Queries;

/// <summary>A span query matching a single term.</summary>
public sealed class SpanTermQuery : SpanQuery
{
    public override string Field { get; }
    public string Term { get; }

    /// <summary>Cached qualified term to avoid repeated string.Concat.</summary>
    internal string? CachedQualifiedTerm { get; set; }

    public SpanTermQuery(string field, string term)
    {
        Field = field;
        Term = term;
    }

    public override bool Equals(object? obj) =>
        obj is SpanTermQuery other &&
        string.Equals(Field, other.Field, StringComparison.Ordinal) &&
        string.Equals(Term, other.Term, StringComparison.Ordinal) &&
        Boost == other.Boost;

    public override int GetHashCode() => CombineBoost(HashCode.Combine(nameof(SpanTermQuery), Field, Term));
}
