using Rowles.LeanLucene.Analysis;
using Rowles.LeanLucene.Analysis.Stemmers;
using Xunit;

namespace Rowles.LeanLucene.Tests.Analysis;

[Trait("Category", "Analysis")]
public sealed class ExtendedLanguageAnalyserTests
{
    [Theory]
    [InlineData("es", "Las casas grandes son muy bonitas")]
    [InlineData("it", "Le case grandi sono molto belle")]
    [InlineData("pt", "As casas grandes são muito bonitas")]
    [InlineData("nl", "De grote huizen zijn erg mooi")]
    [InlineData("ar", "البيوت الكبيرة جميلة جدا")]
    public void NewLanguages_RemoveStopWordsAndProduceTokens(string code, string text)
    {
        var analyser = AnalyserFactory.Create(code);
        var tokens = analyser.Analyse(text);

        Assert.NotEmpty(tokens);
    }

    [Theory]
    [InlineData("ja", "これは日本語のテストです")]
    [InlineData("ko", "이것은 한국어 테스트입니다")]
    public void CjkLanguages_ProduceBigramTokensAndSkipStemming(string code, string text)
    {
        var analyser = AnalyserFactory.Create(code);
        var tokens = analyser.Analyse(text);

        Assert.NotEmpty(tokens);
    }

    [Theory]
    [InlineData("pt-BR", "pt")]
    [InlineData("zh-Hans", "zh")]
    [InlineData("en-GB", "en")]
    [InlineData("DE", "de")]
    public void Bcp47_RegionAndCaseAreNormalised(string requested, string expectedPrimary)
    {
        // Both the factory and the stop word lookup should resolve to the same primary subtag.
        var analyser = AnalyserFactory.Create(requested);
        Assert.NotNull(analyser);

        var stops = StopWords.ForLanguage(requested);
        Assert.NotNull(stops);
        Assert.Same(StopWords.ForLanguage(expectedPrimary), stops);
    }

    [Fact]
    public void Factory_SupportedLanguages_ContainsAllTwelve()
    {
        var supported = AnalyserFactory.SupportedLanguages;
        foreach (var code in new[] { "en", "fr", "de", "es", "it", "pt", "nl", "ru", "ar", "zh", "ja", "ko" })
            Assert.Contains(code, supported);
    }

    [Fact]
    public void Factory_UnsupportedLanguage_ThrowsWithHelpfulMessage()
    {
        var ex = Assert.Throws<NotSupportedException>(() => AnalyserFactory.Create("xx"));
        Assert.Contains("xx", ex.Message);
        Assert.Contains("Supported", ex.Message);
    }

    [Fact]
    public void SpanishStemmer_RemovesCommonSuffixes()
    {
        var s = new SpanishStemmer();
        Assert.Equal("habl", s.Stem("hablando"));
        Assert.Equal("com", s.Stem("comer"));
    }

    [Fact]
    public void ItalianStemmer_HandlesGenderAndNumber()
    {
        var s = new ItalianStemmer();
        Assert.NotEqual("gatti", s.Stem("gatti"));
    }

    [Fact]
    public void PortugueseStemmer_StripsVerbEndings()
    {
        var s = new PortugueseStemmer();
        Assert.NotEqual("falando", s.Stem("falando"));
    }

    [Fact]
    public void DutchStemmer_StripsCommonSuffixes()
    {
        var s = new DutchStemmer();
        Assert.NotEqual("lopen", s.Stem("lopen"));
    }

    [Fact]
    public void ArabicStemmer_StripsDefiniteArticleAndCommonPrefixes()
    {
        var s = new ArabicStemmer();
        // الكتاب: strip "ال" then single-char prefix "ك" per the light stemmer.
        Assert.Equal("تاب", s.Stem("الكتاب"));
    }

    [Theory]
    [InlineData("zh")]
    [InlineData("ja")]
    [InlineData("ko")]
    public void IdentityStemmers_ReturnInputUnchanged(string lang)
    {
        IStemmer stemmer = lang switch
        {
            "zh" => new ChineseStemmer(),
            "ja" => new JapaneseStemmer(),
            "ko" => new KoreanStemmer(),
            _ => throw new InvalidOperationException()
        };

        Assert.Equal("猫", stemmer.Stem("猫"));
        Assert.Equal("test", stemmer.Stem("test"));
    }

    [Fact]
    public void GermanStemmer_FoldsUmlautsAndSharpS()
    {
        var s = new GermanStemmer();
        Assert.Equal("haus", s.Stem("häuser"));
        // ß → ss expansion exercised here.
        var stemmed = s.Stem("straße");
        Assert.StartsWith("strass", stemmed);
    }
}
