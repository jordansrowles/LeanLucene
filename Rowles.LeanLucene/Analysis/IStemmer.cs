namespace Rowles.LeanLucene.Analysis;

/// <summary>
/// Stemming contract. Implementations reduce a word to its root form.
/// </summary>
public interface IStemmer
{
    /// <summary>Returns the stemmed form of the word.</summary>
    string Stem(string word);
}
