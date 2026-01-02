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
    private readonly List<(int Start, int End)> _offsetBuf = new();

    public List<Token> Analyse(ReadOnlySpan<char> input)
    {
        _tokeniser.TokeniseOffsets(input, _offsetBuf);

        var tokens = new List<Token>(_offsetBuf.Count);
        for (int i = 0; i < _offsetBuf.Count; i++)
        {
            var (start, end) = _offsetBuf[i];
            var span = input.Slice(start, end - start);

            // Zero-alloc stop word check using span overload
            if (_stopWordFilter.IsStopWord(span))
                continue;

            int len = end - start;
            if (len > _lowerBuf.Length)
                _lowerBuf = new char[Math.Max(_lowerBuf.Length * 2, len)];

            span.ToLowerInvariant(_lowerBuf.AsSpan(0, len));

            bool changed = !span.SequenceEqual(_lowerBuf.AsSpan(0, len));
            var text = changed
                ? new string(_lowerBuf.AsSpan(0, len))
                : span.ToString();

            tokens.Add(new Token(text, start, end));
        }

        return tokens;
    }
}
