---
_layout: landing
---

# LeanLucene

A fast, embeddable, zero-dependency full-text search engine for .NET — built from the ground up.

## Features

- Memory-mapped index with BM25 scoring
- Compound file support and index-time sorting
- Rich query types: term, boolean, phrase, prefix, fuzzy, wildcard
- Block-join queries for nested documents
- Built-in analysis pipeline (tokenisers, filters, stemmers)
- `System.Diagnostics` observability: activities, metrics, and structured logs

## Getting Started

Add the library to your project:

```bash
dotnet add package Rowles.LeanLucene
```

Index some documents:

```csharp
using var writer = new IndexWriter(new MMapDirectory("./index"), new IndexWriterConfig());

var doc = new LeanDocument();
doc.Add(new TextField("title", "Hello, world"));
writer.AddDocument(doc);
writer.Commit();
```

Run a search:

```csharp
using var searcher = new IndexSearcher(new MMapDirectory("./index"));
var results = searcher.Search(new TermQuery("title", "hello"), 10);
```

## API Reference

Browse the full API in the [API section](api/).
