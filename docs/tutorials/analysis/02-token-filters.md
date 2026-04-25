# Token filters and custom pipelines

A custom analyser is a tokeniser plus zero or more token filters and char filters.

## Build one

```csharp
using Rowles.LeanLucene.Analysis;
using Rowles.LeanLucene.Analysis.Tokenisers;
using Rowles.LeanLucene.Analysis.Filters;

var analyser = new Analyser(
    tokeniser: new Tokeniser(),
    new LowercaseFilter(),
    new StopWordFilter(StopWords.English),
    new PorterStemmerFilter());
```

## Available filters

The library ships filters such as `LowercaseFilter`, `StopWordFilter`,
`AccentFoldingFilter`, `PorterStemmerFilter`, and `SynonymGraphFilter`. Stemmers
per language live under `Analysis.Stemmers`.

## Char filters

Char filters mutate the input character stream before tokenisation. Common uses:
strip HTML (`HtmlStripCharFilter`), apply mapping rules (`MappingCharFilter`),
or pattern replacements (`PatternReplaceCharFilter`). Attach them via
`IndexWriterConfig.CharFilters` (writer-wide).

## Order matters

Filters run left to right. Lowercase before stopwords, stem after both. A
mis-ordered pipeline silently drops or keeps the wrong tokens.

## See also

- <xref:Rowles.LeanLucene.Analysis.Analyser>
- <xref:Rowles.LeanLucene.Analysis.ITokenFilter>
