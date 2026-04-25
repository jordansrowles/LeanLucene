# Built-in analysers

An analyser turns text into a stream of tokens. The same analyser should be used at
index-time and query-time so terms line up.

## Three you'll meet first

```csharp
using Rowles.LeanLucene.Analysis;

var standard = new StandardAnalyser();      // lowercase + tokenise
var stemmed  = new StemmedAnalyser();       // standard + Porter2 (English)
var french   = AnalyserFactory.Create("fr"); // language-specific
```

## AnalyserFactory languages

`AnalyserFactory.Create(string)` accepts BCP 47 codes. Region and script subtags are
stripped (`en-GB` becomes `en`).

Supported: `en`, `fr`, `de`, `es`, `it`, `pt`, `nl`, `ru`, `ar`, `zh`, `ja`, `ko`.

CJK languages (`zh`, `ja`, `ko`) use bigram tokenisation and skip stemming. Anything
else throws `NotSupportedException`.

## Picking an analyser per field

Set the writer-wide default through `IndexWriterConfig.DefaultAnalyser`. To override
per-field, attach an `IAnalyser` to a <xref:Rowles.LeanLucene.Index.Indexer.FieldMapping>
inside an <xref:Rowles.LeanLucene.Index.Indexer.IndexSchema>.

## Inspecting tokens

```csharp
foreach (var token in standard.Analyse("The Quick Brown Foxes".AsSpan()))
    Console.WriteLine(token.Text);
// the, quick, brown, foxes
```

## See also

- <xref:Rowles.LeanLucene.Analysis.AnalyserFactory>
- <xref:Rowles.LeanLucene.Analysis.IAnalyser>
