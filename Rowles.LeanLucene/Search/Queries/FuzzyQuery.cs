namespace Rowles.LeanLucene.Search.Queries;

/// <summary>
/// Matches terms within a Levenshtein edit distance of the target term.
/// </summary>
public sealed class FuzzyQuery : Query
{
    public override string Field { get; }
    public string Term { get; }
    public int MaxEdits { get; }

    public FuzzyQuery(string field, string term, int maxEdits = 2)
    {
        Field = field;
        Term = term;
        MaxEdits = maxEdits;
    }

    public override bool Equals(object? obj) =>
        obj is FuzzyQuery other &&
        string.Equals(Field, other.Field, StringComparison.Ordinal) &&
        string.Equals(Term, other.Term, StringComparison.Ordinal) &&
        MaxEdits == other.MaxEdits && Boost == other.Boost;

    public override int GetHashCode() => CombineBoost(HashCode.Combine(nameof(FuzzyQuery), Field, Term, MaxEdits));
}
