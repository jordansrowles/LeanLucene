namespace Rowles.LeanLucene.Analysis.Analysers;

/// <summary>
/// Analyser that splits text only on whitespace and applies no token filters.
/// </summary>
public sealed class WhitespaceAnalyser : IAnalyser
{
    private readonly WhitespaceTokeniser _tokeniser = new();

    /// <inheritdoc/>
    public List<Token> Analyse(ReadOnlySpan<char> input) => _tokeniser.Tokenise(input);
}
