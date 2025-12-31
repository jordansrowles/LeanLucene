namespace Rowles.LeanLucene.Analysis;

/// <summary>
/// Default analyser combining tokenisation, lowercase normalisation,
/// and stop-word removal into a single pipeline. Uses original input offsets
/// for lowercasing to avoid double string allocation.
/// </summary>
public sealed class StandardAnalyser : IAnalyser
{
    private readonly Tokeniser _tokeniser = new();
    private readonly StopWordFilter _stopWordFilter = new();
    private char[] _lowerBuf = new char[64];

    public List<Token> Analyse(ReadOnlySpan<char> input)
    {
        var tokens = _tokeniser.Tokenise(input);

        // Single pass: lowercase from original input offsets, filter stop words
        int writeIndex = 0;
        for (int i = 0; i < tokens.Count; i++)
        {
            var t = tokens[i];

            if (_stopWordFilter.IsStopWord(t.Text))
                continue;

            // Lowercase directly from input offsets into reusable buffer
            int len = t.EndOffset - t.StartOffset;
            if (len > _lowerBuf.Length)
                _lowerBuf = new char[Math.Max(_lowerBuf.Length * 2, len)];

            var span = input.Slice(t.StartOffset, len);
            span.ToLowerInvariant(_lowerBuf.AsSpan(0, len));

            // Check if lowering actually changed anything
            bool changed = !span.SequenceEqual(_lowerBuf.AsSpan(0, len));
            if (changed)
                tokens[writeIndex] = new Token(new string(_lowerBuf.AsSpan(0, len)), t.StartOffset, t.EndOffset);
            else
                tokens[writeIndex] = t; // Reuse original token text — no extra alloc

            writeIndex++;
        }

        // Trim excess in place
        if (writeIndex < tokens.Count)
            tokens.RemoveRange(writeIndex, tokens.Count - writeIndex);

        return tokens;
    }
}
