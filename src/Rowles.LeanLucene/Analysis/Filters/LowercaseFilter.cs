namespace Rowles.LeanLucene.Analysis.Filters;

/// <summary>
/// Performs an in-place lowercase transformation on tokens or a character buffer.
/// </summary>
public sealed class LowercaseFilter : ITokenFilter
{
    public void Apply(List<Token> tokens)
    {
        for (int i = 0; i < tokens.Count; i++)
        {
            var t = tokens[i];
            var lower = t.Text.ToLowerInvariant();
            if (!ReferenceEquals(lower, t.Text))
                tokens[i] = new Token(lower, t.StartOffset, t.EndOffset);
        }
    }

    public void Apply(Span<char> buffer)
    {
        for (int i = 0; i < buffer.Length; i++)
            buffer[i] = char.ToLowerInvariant(buffer[i]);
    }
}
