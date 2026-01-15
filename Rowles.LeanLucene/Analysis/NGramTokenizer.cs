namespace Rowles.LeanLucene.Analysis;

/// <summary>
/// Splits text into all contiguous character substrings of length in [<see cref="MinGram"/>, <see cref="MaxGram"/>].
/// Useful for partial-word matching and CJK text.
/// 
/// Thread-safety: This class maintains an instance-level intern cache (_internCache) for performance.
/// Each instance should be used by a single thread, or callers should create separate instances per thread.
/// </summary>
public sealed class NGramTokenizer : ITokeniser
{
    public int MinGram { get; }
    public int MaxGram { get; }
    
    // Intern cache to reduce string allocations for repeated n-grams
    private readonly Dictionary<int, string> _internCache = new();
    private const int MaxInternCacheSize = 2048;

    public NGramTokenizer(int minGram, int maxGram)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(minGram, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxGram, minGram);
        MinGram = minGram;
        MaxGram = maxGram;
    }

    public List<Token> Tokenise(ReadOnlySpan<char> input)
    {
        var tokens = new List<Token>();
        int len = input.Length;
        for (int start = 0; start < len; start++)
        {
            for (int gramLen = MinGram; gramLen <= MaxGram && start + gramLen <= len; gramLen++)
            {
                var span = input.Slice(start, gramLen);
                string text = GetOrCacheString(span);
                tokens.Add(new Token(text, start, start + gramLen));
            }
        }
        return tokens;
    }

    private string GetOrCacheString(ReadOnlySpan<char> span)
    {
        int hash = string.GetHashCode(span, StringComparison.Ordinal);
        if (_internCache.TryGetValue(hash, out var cached) && cached.AsSpan().SequenceEqual(span))
            return cached;

        var text = span.ToString();
        if (_internCache.Count < MaxInternCacheSize)
            _internCache[hash] = text;
        return text;
    }
}

/// <summary>
/// Splits text into character substrings of length [<see cref="MinGram"/>, <see cref="MaxGram"/>]
/// anchored at the start of each whitespace-delimited token (edge n-grams).
/// 
/// Thread-safety: This class maintains an instance-level intern cache (_internCache) for performance.
/// Each instance should be used by a single thread, or callers should create separate instances per thread.
/// </summary>
public sealed class EdgeNGramTokenizer : ITokeniser
{
    public int MinGram { get; }
    public int MaxGram { get; }
    
    // Intern cache to reduce string allocations for repeated edge n-grams
    private readonly Dictionary<int, string> _internCache = new();
    private const int MaxInternCacheSize = 2048;

    public EdgeNGramTokenizer(int minGram, int maxGram)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(minGram, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxGram, minGram);
        MinGram = minGram;
        MaxGram = maxGram;
    }

    public List<Token> Tokenise(ReadOnlySpan<char> input)
    {
        var tokens = new List<Token>();
        int len = input.Length;
        int tokenStart = 0;

        for (int i = 0; i <= len; i++)
        {
            bool boundary = i == len || input[i] == ' ' || input[i] == '\t'
                            || input[i] == '\r' || input[i] == '\n';
            if (!boundary) continue;

            int tokenEnd = i;
            int tokenLen = tokenEnd - tokenStart;
            if (tokenLen > 0)
            {
                for (int gramLen = MinGram; gramLen <= MaxGram && gramLen <= tokenLen; gramLen++)
                {
                    var span = input.Slice(tokenStart, gramLen);
                    string text = GetOrCacheString(span);
                    tokens.Add(new Token(text, tokenStart, tokenStart + gramLen));
                }
            }
            tokenStart = i + 1;
        }
        return tokens;
    }

    private string GetOrCacheString(ReadOnlySpan<char> span)
    {
        int hash = string.GetHashCode(span, StringComparison.Ordinal);
        if (_internCache.TryGetValue(hash, out var cached) && cached.AsSpan().SequenceEqual(span))
            return cached;

        var text = span.ToString();
        if (_internCache.Count < MaxInternCacheSize)
            _internCache[hash] = text;
        return text;
    }
}
