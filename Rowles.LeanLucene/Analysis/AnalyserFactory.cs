using Rowles.LeanLucene.Analysis.Stemmers;
using Rowles.LeanLucene.Analysis.Tokenisers;

namespace Rowles.LeanLucene.Analysis;

/// <summary>
/// Factory for creating language-specific analysers.
/// </summary>
public static class AnalyserFactory
{
    /// <summary>
    /// Creates an analyser configured for the specified language.
    /// </summary>
    /// <param name="languageCode">ISO 639-1 language code (en, fr, de, ru, zh).</param>
    /// <returns>A configured analyser for the language.</returns>
    /// <exception cref="NotSupportedException">Thrown for unsupported language codes.</exception>
    public static IAnalyser Create(string languageCode)
    {
        ArgumentNullException.ThrowIfNull(languageCode);

        return languageCode.ToLowerInvariant() switch
        {
            "en" => new LanguageAnalyser(
                new Tokeniser(),
                StopWords.English,
                new EnglishStemmer()),
            "fr" => new LanguageAnalyser(
                new Tokeniser(),
                StopWords.French,
                new FrenchStemmer()),
            "de" => new LanguageAnalyser(
                new Tokeniser(),
                StopWords.German,
                new GermanStemmer()),
            "ru" => new LanguageAnalyser(
                new Tokeniser(),
                StopWords.Russian,
                new RussianStemmer()),
            "zh" => new LanguageAnalyser(
                new CJKBigramTokeniser(),
                StopWords.Chinese,
                stemmer: null),
            _ => throw new NotSupportedException(
                $"Language '{languageCode}' is not supported. Supported: en, fr, de, ru, zh.")
        };
    }

    /// <summary>
    /// Returns all supported language codes.
    /// </summary>
    public static IReadOnlyList<string> SupportedLanguages { get; } = ["en", "fr", "de", "ru", "zh"];
}
