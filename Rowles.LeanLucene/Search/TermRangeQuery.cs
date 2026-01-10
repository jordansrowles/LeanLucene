namespace Rowles.LeanLucene.Search;

/// <summary>
/// Matches documents whose field value falls within a lexicographic range.
/// Bounds can be inclusive or exclusive; <c>null</c> means unbounded.
/// </summary>
public sealed class TermRangeQuery : Query
{
    public override string Field { get; }
    public string? LowerTerm { get; }
    public string? UpperTerm { get; }
    public bool IncludeLower { get; }
    public bool IncludeUpper { get; }

    public TermRangeQuery(string field, string? lowerTerm, string? upperTerm,
        bool includeLower = true, bool includeUpper = true)
    {
        Field = field;
        LowerTerm = lowerTerm;
        UpperTerm = upperTerm;
        IncludeLower = includeLower;
        IncludeUpper = includeUpper;
    }
}
