# Spelling suggestions

`DidYouMeanSuggester` returns alternative spellings for a term, ranked by document
frequency divided by edit distance.

## Quick path

```csharp
using Rowles.LeanLucene.Search.Suggestions;

var suggestions = DidYouMeanSuggester.Suggest(
    searcher, field: "title", queryTerm: "lukcy",
    maxEdits: 2, topN: 5);

foreach (var s in suggestions)
    Console.WriteLine($"{s.Term} (distance={s.Distance}, df={s.DocFreq})");
```

The underlying <xref:Rowles.LeanLucene.Search.Suggestions.SpellIndex> is built once
per searcher / field and cached for subsequent calls.

## Reusing a SpellIndex

For repeated suggestions over the same field, build the index explicitly:

```csharp
var spell = SpellIndex.Build(searcher, "title");
var s1 = DidYouMeanSuggester.Suggest(spell, "lukcy", maxEdits: 2, topN: 5);
var s2 = DidYouMeanSuggester.Suggest(spell, "frmo",  maxEdits: 2, topN: 5);
```

This avoids re-scanning the term dictionary.

## Tuning

- `maxEdits` — Levenshtein cap (sensible values 1–2).
- `topN` — number of suggestions returned.

## See also

- <xref:Rowles.LeanLucene.Search.Suggestions.DidYouMeanSuggester>
- <xref:Rowles.LeanLucene.Search.Suggestions.SpellIndex>
