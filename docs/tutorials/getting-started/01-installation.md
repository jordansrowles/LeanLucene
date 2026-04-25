# Installation and first index

## Install

```bash
dotnet add package Rowles.LeanLucene
```

Targets `net10.0` and `net11.0`.

## Index two documents

```csharp
using Rowles.LeanLucene.Document;
using Rowles.LeanLucene.Document.Fields;
using Rowles.LeanLucene.Index.Indexer;
using Rowles.LeanLucene.Store;

using var dir = new MMapDirectory("./index");
using var writer = new IndexWriter(dir, new IndexWriterConfig());

var doc1 = new LeanDocument();
doc1.Add(new TextField("title", "The quick brown fox"));
doc1.Add(new StringField("id", "1"));
writer.AddDocument(doc1);

var doc2 = new LeanDocument();
doc2.Add(new TextField("title", "Lazy dogs and slow cats"));
doc2.Add(new StringField("id", "2"));
writer.AddDocument(doc2);

writer.Commit();
```

## Search

```csharp
using Rowles.LeanLucene.Search.Queries;
using Rowles.LeanLucene.Search.Searcher;

using var searcher = new IndexSearcher(dir);
var hits = searcher.Search(new TermQuery("title", "fox"), topN: 10);

foreach (var hit in hits.ScoreDocs)
    Console.WriteLine($"docId={hit.DocId} score={hit.Score}");
```

## Layout on disk

The directory holds segment files (`*.seg`, `*.dic`, `*.pos`, `*.fdt`, `*.fdx`, `*.nrm`, etc.)
and one or more `segments_N` commit files.

## See also

- <xref:Rowles.LeanLucene.Index.Indexer.IndexWriter>
- <xref:Rowles.LeanLucene.Search.Searcher.IndexSearcher>
