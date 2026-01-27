using System.Text.RegularExpressions;

namespace Rowles.LeanLucene.Analysis.Filters;

/// <summary>
/// Replaces text matching a regex pattern with a replacement string.
/// </summary>
public sealed class PatternReplaceCharFilter : ICharFilter
{
    private readonly Regex _pattern;
    private readonly string _replacement;

    public PatternReplaceCharFilter(string pattern, string replacement)
    {
        _pattern = new Regex(pattern, RegexOptions.Compiled);
        _replacement = replacement;
    }

    public string Filter(ReadOnlySpan<char> input)
        => _pattern.Replace(input.ToString(), _replacement);
}
