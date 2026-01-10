namespace Rowles.LeanLucene.Analysis;

/// <summary>
/// Splits text into all contiguous character substrings of length in [<see cref="MinGram"/>, <see cref="MaxGram"/>].
/// Useful for partial-word matching and CJK text.
/// </summary>
public sealed class NGramTokenizer : ITokeniser
{
    public int MinGram { get; }
    public int MaxGram { get; }

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
                tokens.Add(new Token(input.Slice(start, gramLen).ToString(), start, start + gramLen));
        }
        return tokens;
    }
}

/// <summary>
/// Splits text into character substrings of length [<see cref="MinGram"/>, <see cref="MaxGram"/>]
/// anchored at the start of each whitespace-delimited token (edge n-grams).
/// </summary>
public sealed class EdgeNGramTokenizer : ITokeniser
{
    public int MinGram { get; }
    public int MaxGram { get; }

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
                    tokens.Add(new Token(input.Slice(tokenStart, gramLen).ToString(), tokenStart, tokenStart + gramLen));
            }
            tokenStart = i + 1;
        }
        return tokens;
    }
}
