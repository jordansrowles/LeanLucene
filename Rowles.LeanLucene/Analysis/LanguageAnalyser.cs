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

    public LanguageAnalyser(ITokeniser tokeniser, IEnumerable<string>? stopWords, IStemmer? stemmer)
    {
        _tokeniser = tokeniser ?? throw new ArgumentNullException(nameof(tokeniser));
        _stopWordFilter = new StopWordFilter(stopWords);
        _stemmer = stemmer;
    }

    public List<Token> Analyse(ReadOnlySpan<char> input)
    {
        var rawTokens = _tokeniser.Tokenise(input);
        var result = new List<Token>(rawTokens.Count);

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

            result.Add(new Token(text, t.StartOffset, t.EndOffset));
        }

        return result;
    }
}
