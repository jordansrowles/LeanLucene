namespace Rowles.LeanLucene.Analysis;

/// <summary>
/// Composable analyser that runs a tokeniser followed by a chain of filters.
/// </summary>
public sealed class Analyser : IAnalyser
{
    private readonly ITokeniser _tokeniser;
    private readonly ITokenFilter[] _filters;

    public Analyser(ITokeniser tokeniser, params ITokenFilter[] filters)
    {
        _tokeniser = tokeniser;
        _filters = filters;
    }

    public List<Token> Analyse(ReadOnlySpan<char> input)
    {
        var tokens = _tokeniser.Tokenise(input);
        foreach (var filter in _filters)
            filter.Apply(tokens);
        return tokens;
    }
}
