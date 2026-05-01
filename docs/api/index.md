---
uid: api
title: API Reference
---

# API Reference

Welcome to the LeanLucene API reference. Browse the namespaces in the left-hand
navigation, or jump straight to common entry points below.

Items marked with a lock icon are internal APIs. They are documented for contributors and may
change between releases.

## Common entry points

- <xref:Rowles.LeanLucene.Index.Indexer.IndexWriter> — write documents to an index.
- <xref:Rowles.LeanLucene.Search.Searcher.IndexSearcher> — open an index and run queries.
- <xref:Rowles.LeanLucene.Document.LeanDocument> — the document type used for both indexing and retrieval.

## Namespaces

| Namespace | Purpose |
|---|---|
| `Rowles.LeanLucene` | Top-level types: documents, fields, configuration. |
| `Rowles.LeanLucene.Analysis` | Analysers, tokenisers, token filters. |
| `Rowles.LeanLucene.Codecs` | On-disk format: postings, doc values, stored fields. |
| `Rowles.LeanLucene.Diagnostics` | Metrics collectors, activity sources, slow query log. |
| `Rowles.LeanLucene.Index` | Indexing primitives, segment management, merge policy. |
| `Rowles.LeanLucene.Search` | Queries, scoring, top-N collection. |
| `Rowles.LeanLucene.Store` | Memory-mapped IO and locking. |
