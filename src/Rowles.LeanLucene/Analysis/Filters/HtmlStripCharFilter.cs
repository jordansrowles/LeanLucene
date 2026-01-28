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
