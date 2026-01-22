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
        // Compile once; anchor to full term match automatically via Regex.IsMatch behaviour.
        // Callers should use ^ / $ anchors in the pattern if full-string matching is desired.
        CompiledRegex = new Regex(pattern, options | RegexOptions.Compiled, TimeSpan.FromSeconds(1));
    }
}
