namespace Rowles.LeanLucene.Analysis;

/// <summary>
/// Default analyser combining tokenisation, lowercase normalisation,
/// and stop-word removal into a single pipeline. Uses original input offsets
/// for lowercasing to avoid double string allocation.
/// The returned token list is reused across calls — callers must not hold
/// references to it beyond the current invocation.
/// </summary>
public sealed class StandardAnalyser : IAnalyser
{
    private readonly Tokeniser _tokeniser = new();
    private readonly StopWordFilter _stopWordFilter = new();
    private char[] _lowerBuf = new char[64];
    private readonly List<(int Start, int End)> _offsetBuf = new();
    private readonly List<Token> _tokensBuf = new();

    public List<Token> Analyse(ReadOnlySpan<char> input)
    {
        _tokeniser.TokeniseOffsets(input, _offsetBuf);

        _tokensBuf.Clear();
        if (_tokensBuf.Capacity < _offsetBuf.Count)
            _tokensBuf.Capacity = _offsetBuf.Count;

        for (int i = 0; i < _offsetBuf.Count; i++)
        {
            var (start, end) = _offsetBuf[i];
            var span = input.Slice(start, end - start);

            if (_stopWordFilter.IsStopWord(span))
                continue;

            int len = end - start;
            if (len > _lowerBuf.Length)
                _lowerBuf = new char[Math.Max(_lowerBuf.Length * 2, len)];

            span.ToLowerInvariant(_lowerBuf.AsSpan(0, len));
            var text = new string(_lowerBuf.AsSpan(0, len));

            _tokensBuf.Add(new Token(text, start, end));
        }

        return _tokensBuf;
    }
}
