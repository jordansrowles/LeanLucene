namespace Rowles.LeanCorpus.Analysis.Tokenisers;

using Rowles.LeanCorpus.Analysis;
using Rowles.LeanCorpus.Analysis.Analysers;

/// <summary>
/// Splits text into all contiguous character substrings of length in [<see cref="MinGram"/>, <see cref="MaxGram"/>].
/// Useful for partial-word matching and CJK text.
///
/// When <see cref="SplitOnWhitespace"/> is <see langword="true"/> the tokeniser first splits on
/// <c>' '</c>, <c>'\t'</c>, <c>'\r'</c>, and <c>'\n'</c> and applies n-grams per word only,
/// which avoids cross-word-boundary grams and substantially reduces allocations.
///
/// Thread-safety: This class maintains an instance-level intern cache for performance.
/// Each instance should be used by a single thread, or callers should create separate instances per thread.
/// </summary>
public sealed class NGramTokeniser : ITokeniser
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
                tokens.Add(new Token(_textCache.GetOrAdd(span), start, start + gramLen));
            }
        }
    }

    private void FillSplit(ReadOnlySpan<char> input, List<Token> tokens)
    {
        int len = input.Length;
        int wordStart = 0;

        for (int i = 0; i <= len; i++)
        {
            if (i != len && input[i] != ' ' && input[i] != '\t' && input[i] != '\r' && input[i] != '\n')
                continue;

            int wordLen = i - wordStart;
            if (wordLen > 0)
            {
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

            wordStart = i + 1;
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
