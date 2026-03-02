namespace Rowles.LeanLucene.Search.Queries;

/// <summary>
/// Matches terms within a Levenshtein edit distance of the target term.
/// </summary>
public sealed class FuzzyQuery : Query
{
    /// <summary>Default maximum number of matching terms to expand.</summary>
    public const int DefaultMaxExpansions = 64;

    public override string Field { get; }
    public string Term { get; }
    public int MaxEdits { get; }

    /// <summary>
    /// Maximum number of matching terms to expand into the scored query.
    /// When more terms match, only the closest (lowest edit distance) are kept.
    /// </summary>
    public int MaxExpansions { get; }

    public FuzzyQuery(string field, string term, int maxEdits = 2, int maxExpansions = DefaultMaxExpansions)
    {
        Field = field;
        Term = term;
        MaxEdits = maxEdits;
        MaxExpansions = maxExpansions;
    }

    public override bool Equals(object? obj) =>
        obj is FuzzyQuery other &&
        string.Equals(Field, other.Field, StringComparison.Ordinal) &&
        string.Equals(Term, other.Term, StringComparison.Ordinal) &&
        MaxEdits == other.MaxEdits && MaxExpansions == other.MaxExpansions && Boost == other.Boost;

    public override int GetHashCode() => CombineBoost(HashCode.Combine(nameof(FuzzyQuery), Field, Term, MaxEdits, MaxExpansions));
}
