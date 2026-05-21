namespace Rowles.LeanCorpus.Analysis.Tokenisers;

using Rowles.LeanCorpus.Analysis;
using Rowles.LeanCorpus.Analysis.Analysers;

/// <summary>
/// Splits text into all contiguous character substrings of length in [<see cref="MinGram"/>, <see cref="MaxGram"/>].
/// Useful for partial-word matching and CJK text.
///
/// When <see cref="SplitOnWhitespace"/> is <see langword="true"/> the tokeniser first splits on
/// whitespace (via <see cref="char.IsWhiteSpace(char)"/>) and applies n-grams per word only,
/// which avoids cross-word-boundary grams and substantially reduces allocations.
///
/// Thread-safety: The span path is thread-safe for concurrent use on the same instance.
/// The legacy <see cref="List{Token}"/> path uses an instance-level intern cache; each thread
/// should use a separate instance when calling <see cref="Tokenise(ReadOnlySpan{char})"/> concurrently.
/// </summary>
public sealed class NGramTokeniser : ITokeniser, ISpanTokeniser
{
    /// <summary>
    /// Gets the minimum n-gram length (inclusive).
    /// </summary>
    public int MinGram { get; }

    /// <summary>
    /// Gets the maximum n-gram length (inclusive).
    /// </summary>
    public int MaxGram { get; }

    /// <summary>
    /// Gets whether the tokeniser splits on whitespace before applying n-grams.
    /// When <see langword="true"/>, no gram spans a word boundary.
    /// </summary>
    public bool SplitOnWhitespace { get; }

    private const int MaxTextCacheSize = 65_536;
    private readonly TokenTextCache _textCache = new(MaxTextCacheSize);
    private readonly WhitespaceTokeniser _ws = new();

    /// <summary>
    /// Initialises a new <see cref="NGramTokeniser"/> with the specified gram size range.
    /// </summary>
    /// <param name="minGram">The minimum gram length (must be ≥ 1).</param>
    /// <param name="maxGram">The maximum gram length (must be ≥ <paramref name="minGram"/>).</param>
    /// <param name="splitOnWhitespace">
    /// When <see langword="true"/>, n-grams are generated per whitespace-delimited word rather than
    /// across the entire input. Defaults to <see langword="false"/> for backward compatibility.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="minGram"/> is less than 1, or <paramref name="maxGram"/> is less than <paramref name="minGram"/>.
    /// </exception>
    public NGramTokeniser(int minGram, int maxGram, bool splitOnWhitespace = false)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(minGram, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxGram, minGram);
        MinGram = minGram;
        MaxGram = maxGram;
        SplitOnWhitespace = splitOnWhitespace;
    }

    /// <inheritdoc/>
    public List<Token> Tokenise(ReadOnlySpan<char> input)
    {
        // Pre-size only for the full-text path where the count is O(1).
        var tokens = SplitOnWhitespace
            ? new List<Token>()
            : new List<Token>(CountNGrams(input.Length));
        FillTokens(input, tokens);
        return tokens;
    }

    /// <summary>
    /// Tokenises the input into the supplied destination list, clearing it before use.
    /// The list's existing capacity is reused; no pre-count pass is performed.
    /// </summary>
    /// <param name="input">The text to tokenise.</param>
    /// <param name="tokens">The destination token buffer to populate.</param>
    public void Tokenise(ReadOnlySpan<char> input, List<Token> tokens)
    {
        ArgumentNullException.ThrowIfNull(tokens);
        tokens.Clear();
        FillTokens(input, tokens);
    }

    /// <inheritdoc/>
    public void Tokenise(ReadOnlySpan<char> input, ISpanTokenSink sink)
    {
        ArgumentNullException.ThrowIfNull(sink);

        if (SplitOnWhitespace)
            EmitSplit(input, sink);
        else
            EmitFull(input, sink);
    }

    /// <summary>
    /// Returns a stack-only <see cref="Enumerator"/> that yields n-gram tokens
    /// one at a time without materialising a <see cref="List{Token}"/> or token text strings.
    /// When <see cref="SplitOnWhitespace"/> is <see langword="true"/>, n-grams are
    /// generated per word; otherwise they span the full input.
    /// Use in a <c>foreach</c> loop for zero-list-allocation enumeration.
    /// </summary>
    /// <param name="input">The text to tokenise.</param>
    public Enumerator EnumerateTokens(ReadOnlySpan<char> input) => new(this, input);

    /// <summary>
    /// Stack-only n-gram enumerator. Each call to <see cref="MoveNext"/> yields the
    /// next n-gram in increasing start-offset order.
    /// </summary>
    public ref struct Enumerator
    {
        private readonly NGramTokeniser _owner;
        private readonly ReadOnlySpan<char> _input;
        private int _wordIdx;
        private int _start;
        private int _gramLen;
        private SpanToken _current;
        private List<(int Start, int End)>? _wordOffsets;

        internal Enumerator(NGramTokeniser owner, ReadOnlySpan<char> input)
        {
            _owner = owner;
            _input = input;
            _wordIdx = 0;
            _start = 0;
            _gramLen = owner.MinGram - 1;
            _current = default;

            if (owner.SplitOnWhitespace)
            {
                _wordOffsets = [];
                owner._ws.TokeniseOffsets(input, _wordOffsets);
            }
        }

        /// <summary>Gets the current token.</summary>
        public SpanToken Current => _current;

        /// <summary>Advances to the next n-gram token.</summary>
        public bool MoveNext()
        {
            if (_owner.SplitOnWhitespace)
                return MoveNextSplit();
            return MoveNextFull();
        }

        private bool MoveNextSplit()
        {
            while (_wordIdx < _wordOffsets!.Count)
            {
                var (wordStart, wordEnd) = _wordOffsets[_wordIdx];
                int wordLen = wordEnd - wordStart;

                _gramLen++;
                if (_gramLen <= _owner.MaxGram && _start + _gramLen <= wordLen)
                {
                    int absStart = wordStart + _start;
                    var span = _input.Slice(absStart, _gramLen);
                    _current = new SpanToken(span, absStart, absStart + _gramLen);
                    return true;
                }

                _start++;
                _gramLen = _owner.MinGram - 1;

                if (_start >= wordLen)
                {
                    _wordIdx++;
                    _start = 0;
                }
            }

            return false;
        }

        private bool MoveNextFull()
        {
            int len = _input.Length;
            while (_start < len)
            {
                _gramLen++;
                if (_gramLen <= _owner.MaxGram && _start + _gramLen <= len)
                {
                    var span = _input.Slice(_start, _gramLen);
                    _current = new SpanToken(span, _start, _start + _gramLen);
                    return true;
                }

                _start++;
                _gramLen = _owner.MinGram - 1;
            }

            return false;
        }

        /// <summary>Returns <c>this</c> for <c>foreach</c> support.</summary>
        public Enumerator GetEnumerator() => this;
    }

    private void EmitFull(ReadOnlySpan<char> input, ISpanTokenSink sink)
    {
        int len = input.Length;
        for (int start = 0; start < len; start++)
        {
            for (int gramLen = MinGram; gramLen <= MaxGram && start + gramLen <= len; gramLen++)
            {
                sink.Add(input.Slice(start, gramLen), start, start + gramLen);
            }
        }
    }

    private void EmitSplit(ReadOnlySpan<char> input, ISpanTokenSink sink)
    {
        List<(int Start, int End)> wordOffsets = [];
        _ws.TokeniseOffsets(input, wordOffsets);

        foreach (var (wordStart, wordEnd) in wordOffsets)
        {
            int wordLen = wordEnd - wordStart;
            for (int start = 0; start < wordLen; start++)
            {
                for (int gramLen = MinGram; gramLen <= MaxGram && start + gramLen <= wordLen; gramLen++)
                {
                    int absStart = wordStart + start;
                    sink.Add(input.Slice(absStart, gramLen), absStart, absStart + gramLen);
                }
            }
        }
    }

    private void FillTokens(ReadOnlySpan<char> input, List<Token> tokens)
    {
        if (SplitOnWhitespace)
            FillSplit(input, tokens);
        else
            FillFull(input, tokens);
    }

    private void FillFull(ReadOnlySpan<char> input, List<Token> tokens)
    {
        int len = input.Length;
        for (int start = 0; start < len; start++)
        {
            for (int gramLen = MinGram; gramLen <= MaxGram && start + gramLen <= len; gramLen++)
            {
                var span = input.Slice(start, gramLen);
                tokens.Add(new Token(TokenTextCache.Allocate(span), start, start + gramLen));
            }
        }
    }

    private void FillSplit(ReadOnlySpan<char> input, List<Token> tokens)
    {
        List<(int Start, int End)> wordOffsets = [];
        _ws.TokeniseOffsets(input, wordOffsets);

        foreach (var (wordStart, wordEnd) in wordOffsets)
        {
            int wordLen = wordEnd - wordStart;
            for (int start = 0; start < wordLen; start++)
            {
                for (int gramLen = MinGram; gramLen <= MaxGram && start + gramLen <= wordLen; gramLen++)
                {
                    int absStart = wordStart + start;
                    var span = input.Slice(absStart, gramLen);
                    tokens.Add(new Token(_textCache.GetOrAdd(span), absStart, absStart + gramLen));
                }
            }
        }
    }

    // O(MaxGram - MinGram): used only by the allocating overload on the full-text path.
    private int CountNGrams(int length)
    {
        int count = 0;
        for (int gramLen = MinGram; gramLen <= MaxGram && gramLen <= length; gramLen++)
            count += length - gramLen + 1;
        return count;
    }
}
