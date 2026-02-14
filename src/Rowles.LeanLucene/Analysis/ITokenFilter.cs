namespace Rowles.LeanLucene.Analysis;

/// <summary>
/// Transforms a token list in place (e.g. lowercase, stop-word removal, stemming).
/// </summary>
public interface ITokenFilter
{
    void Apply(List<Token> tokens);
}
