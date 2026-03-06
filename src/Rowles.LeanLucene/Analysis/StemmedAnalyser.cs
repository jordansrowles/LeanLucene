namespace Rowles.LeanLucene.Analysis;

/// <summary>
/// Extends <see cref="StandardAnalyser"/> with Porter stemming for improved recall.
/// Pipeline: tokenise → lowercase → stop-word removal → Porter stem.
/// </summary>
public sealed class StemmedAnalyser : IAnalyser
{
    private readonly StandardAnalyser _inner = new();
    private readonly PorterStemmerFilter _stemmer = new();

    /// <inheritdoc/>
    public List<Token> Analyse(ReadOnlySpan<char> input)
    {
        var tokens = _inner.Analyse(input);
        _stemmer.Apply(tokens);
        return tokens;
    }
}
