using Rowles.LeanLucene.Analysis.Tokenisers;

namespace Rowles.LeanLucene.Analysis;

/// <summary>
/// Default analyser combining tokenisation, lowercase normalisation,
/// and stop-word removal into a single pipeline. Uses original input offsets
/// for lowercasing to avoid double string allocation.
/// The returned token list is reused across calls — callers must not hold
/// references to it beyond the current invocation.
/// 
/// Thread-safety: This class maintains instance-level buffers (_tokensBuf, _lowerBuf, _internCache)
/// for performance. Each instance should be used by a single thread, or callers should create
/// separate instances per thread (as IndexWriter does in AddDocumentsConcurrent).
/// </summary>
public sealed class StandardAnalyser : IAnalyser
{
    private readonly Tokeniser _tokeniser = new();
    private readonly StopWordFilter _stopWordFilter;
    private char[] _lowerBuf = new char[64];
    private readonly List<(int Start, int End)> _offsetBuf = new();
    private readonly List<Token> _tokensBuf = new();
    // Intern cache for frequently seen token strings to reduce per-token string allocation
    private readonly Dictionary<int, string> _internCache = new();
    private readonly int _maxInternCacheSize;

    public StandardAnalyser(int internCacheSize = 4096, IEnumerable<string>? stopWords = null)
    {
        _maxInternCacheSize = internCacheSize;
        _stopWordFilter = new StopWordFilter(stopWords);
    }

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
            var lowerSpan = _lowerBuf.AsSpan(0, len);

            // Try intern cache to reuse string objects for repeated tokens
            int hash = string.GetHashCode(lowerSpan);
            string text;
            if (_internCache.TryGetValue(hash, out var cached) && cached.AsSpan().SequenceEqual(lowerSpan))
            {
                text = cached;
            }
            else
            {
                text = new string(lowerSpan);
                if (_internCache.Count < _maxInternCacheSize)
                    _internCache[hash] = text;
            }

            _tokensBuf.Add(new Token(text, start, end));
        }

        return _tokensBuf;
    }
}
