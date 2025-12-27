namespace Rowles.LeanLucene.Analysis;

/// <summary>
/// Represents a single token produced by the analysis pipeline,
/// carrying the term text and its character offsets in the original input.
/// </summary>
public readonly struct Token(string text, int startOffset, int endOffset)
{
    public string Text { get; } = text;
    public int StartOffset { get; } = startOffset;
    public int EndOffset { get; } = endOffset;
}
