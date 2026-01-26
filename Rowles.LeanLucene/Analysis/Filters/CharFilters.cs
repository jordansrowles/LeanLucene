using System.Text.RegularExpressions;

namespace Rowles.LeanLucene.Analysis.Filters;

/// <summary>
/// Strips HTML/XML tags from input text, leaving only the text content.
/// </summary>
public sealed class HtmlStripCharFilter : ICharFilter
{
    private static readonly Regex TagPattern = new(@"<[^>]+>", RegexOptions.Compiled);
    private static readonly Regex EntityPattern = new(@"&\w+;", RegexOptions.Compiled);

    public string Filter(ReadOnlySpan<char> input)
    {
        var text = input.ToString();
        text = TagPattern.Replace(text, " ");
        text = EntityPattern.Replace(text, " ");
        return text;
    }
}

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

/// <summary>
/// Maps specific characters or strings to replacements using a lookup table.
/// Useful for normalising special characters (e.g., smart quotes → straight quotes).
/// </summary>
public sealed class MappingCharFilter : ICharFilter
{
    private readonly IReadOnlyDictionary<string, string> _mappings;

    public MappingCharFilter(IReadOnlyDictionary<string, string> mappings)
    {
        _mappings = mappings ?? throw new ArgumentNullException(nameof(mappings));
    }

    public string Filter(ReadOnlySpan<char> input)
    {
        var text = input.ToString();
        foreach (var (from, to) in _mappings)
            text = text.Replace(from, to, StringComparison.Ordinal);
        return text;
    }
}
