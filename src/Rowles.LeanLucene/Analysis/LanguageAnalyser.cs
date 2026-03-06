using Rowles.LeanLucene.Analysis.Stemmers;
using Rowles.LeanLucene.Analysis.Tokenisers;

namespace Rowles.LeanLucene.Analysis;

/// <summary>
/// Configurable analyser that chains a tokeniser, lowercase normalisation,
/// stop-word removal, and optional stemming. Used by <see cref="AnalyserFactory"/>
/// for language-specific analysis pipelines.
/// </summary>
public sealed class LanguageAnalyser : IAnalyser
{
    private readonly ITokeniser _tokeniser;
    private readonly StopWordFilter _stopWordFilter;
    private readonly IStemmer? _stemmer;
    private char[] _lowerBuf = new char[64];
    private readonly List<Token> _resultBuffer = new(32);

    /// <summary>
    /// Initialises a new <see cref="LanguageAnalyser"/> with the specified tokeniser, stop words, and optional stemmer.
    /// </summary>
    /// <param name="tokeniser">The tokeniser used to split input text into raw tokens.</param>
    /// <param name="stopWords">Stop words to remove, or <see langword="null"/> to use the default English list.</param>
    /// <param name="stemmer">Optional stemmer to reduce tokens to their root form, or <see langword="null"/> to skip stemming.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="tokeniser"/> is <see langword="null"/>.</exception>
    public LanguageAnalyser(ITokeniser tokeniser, IEnumerable<string>? stopWords, IStemmer? stemmer)
    {
        _tokeniser = tokeniser ?? throw new ArgumentNullException(nameof(tokeniser));
        _stopWordFilter = new StopWordFilter(stopWords);
        _stemmer = stemmer;
    }

    /// <inheritdoc/>
    public List<Token> Analyse(ReadOnlySpan<char> input)
    {
        var rawTokens = _tokeniser.Tokenise(input);
        _resultBuffer.Clear();

        for (int i = 0; i < rawTokens.Count; i++)
        {
            var t = rawTokens[i];
            var span = t.Text.AsSpan();

            // Lowercase
            int len = span.Length;
            if (len > _lowerBuf.Length)
                _lowerBuf = new char[Math.Max(_lowerBuf.Length * 2, len)];
            span.ToLowerInvariant(_lowerBuf.AsSpan(0, len));
            string text = new string(_lowerBuf, 0, len);

            // Stop-word check
            if (_stopWordFilter.IsStopWord(text))
                continue;

            // Stem
            if (_stemmer is not null)
                text = _stemmer.Stem(text);

            _resultBuffer.Add(new Token(text, t.StartOffset, t.EndOffset));
        }

        return new List<Token>(_resultBuffer);
    }
}
