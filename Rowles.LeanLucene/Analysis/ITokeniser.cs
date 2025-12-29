namespace Rowles.LeanLucene.Analysis;

/// <summary>
/// Splits input text into raw tokens.
/// </summary>
public interface ITokeniser
{
    List<Token> Tokenise(ReadOnlySpan<char> input);
}
