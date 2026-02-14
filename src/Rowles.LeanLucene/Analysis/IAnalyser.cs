namespace Rowles.LeanLucene.Analysis;

/// <summary>
/// Analyses input text into a list of tokens for indexing or querying.
/// </summary>
public interface IAnalyser
{
    List<Token> Analyse(ReadOnlySpan<char> input);
}
