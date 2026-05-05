namespace Rowles.LeanLucene.Analysis.Analysers;

/// <summary>
/// Analyser that treats the complete input as a single token.
/// </summary>
public sealed class KeywordAnalyser : IAnalyser
{
    private readonly KeywordTokeniser _tokeniser = new();

    /// <inheritdoc/>
    public List<Token> Analyse(ReadOnlySpan<char> input) => _tokeniser.Tokenise(input);
}
