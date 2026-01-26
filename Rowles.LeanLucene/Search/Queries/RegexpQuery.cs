using System.Text.RegularExpressions;

namespace Rowles.LeanLucene.Search.Queries;

/// <summary>
/// Matches all documents that contain a term matching the provided regular expression.
/// The regex is applied to the bare term text (not the qualified field\0term form).
/// </summary>
public sealed class RegexpQuery : Query
{
    public override string Field { get; }
    public string Pattern { get; }

    internal Regex CompiledRegex { get; }

    public RegexpQuery(string field, string pattern, RegexOptions options = RegexOptions.None)
    {
        Field = field;
        Pattern = pattern;
        CompiledRegex = new Regex(pattern, options | RegexOptions.Compiled, TimeSpan.FromSeconds(1));
    }

    public override bool Equals(object? obj) =>
        obj is RegexpQuery other &&
        string.Equals(Field, other.Field, StringComparison.Ordinal) &&
        string.Equals(Pattern, other.Pattern, StringComparison.Ordinal) &&
        Boost == other.Boost;

    public override int GetHashCode() => CombineBoost(HashCode.Combine(nameof(RegexpQuery), Field, Pattern));
}
