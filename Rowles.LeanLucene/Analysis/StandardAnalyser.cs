namespace Rowles.LeanLucene.Analysis;

/// <summary>
/// Default analyser combining tokenisation, lowercase normalisation,
/// and stop-word removal into a single pipeline.
/// </summary>
public sealed class StandardAnalyser
{
    private readonly Tokeniser _tokeniser = new();
    private readonly StopWordFilter _stopWordFilter = new();

    public List<Token> Analyse(ReadOnlySpan<char> input)
    {
        var tokens = _tokeniser.Tokenise(input);

        // In-place lowercase and stop-word removal (single pass)
        int writeIndex = 0;
        for (int i = 0; i < tokens.Count; i++)
        {
            var t = tokens[i];

            if (_stopWordFilter.IsStopWord(t.Text))
                continue;

            var lowerText = t.Text.ToLowerInvariant();
            if (!ReferenceEquals(lowerText, t.Text))
                tokens[writeIndex] = new Token(lowerText, t.StartOffset, t.EndOffset);
            else
                tokens[writeIndex] = t;

            writeIndex++;
        }

        // Trim excess in place
        if (writeIndex < tokens.Count)
            tokens.RemoveRange(writeIndex, tokens.Count - writeIndex);

        return tokens;
    }
}
