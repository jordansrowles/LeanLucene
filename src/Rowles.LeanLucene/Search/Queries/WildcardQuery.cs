namespace Rowles.LeanLucene.Search.Queries;

/// <summary>
/// Matches terms using wildcard patterns (* = any chars, ? = single char).
/// </summary>
public sealed class WildcardQuery : Query
{
    public override string Field { get; }
    public string Pattern { get; }

    public WildcardQuery(string field, string pattern)
    {
        Field = field;
        Pattern = pattern;
    }

    public override bool Equals(object? obj) =>
        obj is WildcardQuery other &&
        string.Equals(Field, other.Field, StringComparison.Ordinal) &&
        string.Equals(Pattern, other.Pattern, StringComparison.Ordinal) &&
        Boost == other.Boost;

    public override int GetHashCode() => CombineBoost(HashCode.Combine(nameof(WildcardQuery), Field, Pattern));

    /// <summary>Tests whether a term matches the wildcard pattern.</summary>
    public static bool Matches(ReadOnlySpan<char> term, ReadOnlySpan<char> pattern)
    {
        int t = 0, p = 0;
        int starT = -1, starP = -1;

        while (t < term.Length)
        {
            if (p < pattern.Length && (pattern[p] == '?' || pattern[p] == term[t]))
            {
                t++;
                p++;
            }
            else if (p < pattern.Length && pattern[p] == '*')
            {
                starP = p++;
                starT = t;
            }
            else if (starP >= 0)
            {
                p = starP + 1;
                t = ++starT;
            }
            else
            {
                return false;
            }
        }

        while (p < pattern.Length && pattern[p] == '*')
            p++;

        return p == pattern.Length;
    }
}
