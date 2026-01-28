namespace Rowles.LeanLucene.Analysis.Filters;

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
