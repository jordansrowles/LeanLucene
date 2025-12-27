namespace Rowles.LeanLucene.Analysis;

/// <summary>
/// Performs an in-place lowercase transformation on a character buffer.
/// </summary>
public sealed class LowercaseFilter
{
    public void Apply(Span<char> buffer)
    {
        // In-place lowercase using simple character transform.
        for (int i = 0; i < buffer.Length; i++)
            buffer[i] = char.ToLowerInvariant(buffer[i]);
    }
}
