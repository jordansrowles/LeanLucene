# Vector search

LeanLucene stores dense float vectors in a flat `.vec` file per segment and serves
ANN queries via SIMD cosine similarity.

## Index a vector

```csharp
using Rowles.LeanLucene.Document.Fields;

var doc = new LeanDocument();
doc.Add(new StringField("id", "v1"));
doc.Add(new VectorField("embedding", new float[] { 0.1f, 0.2f, 0.3f, 0.4f }));
writer.AddDocument(doc);
```

All vectors written to the same field must have the same dimensionality.

## Query

```csharp
using Rowles.LeanLucene.Search.Queries;

var query = new VectorQuery("embedding", queryVector, topK: 10);
var hits = searcher.Search(query, topN: 10);
```

The score is cosine similarity, range `[-1, 1]` (typically `[0, 1]` for normalised
vectors).

## Hybrid retrieval

Combine a vector query with a text query through [RRF](04-rrf.md) for hybrid search.

## Implementation note

The current implementation is a flat (brute-force) SIMD scan. Suitable for low-to-
mid millions of vectors per segment.

## See also

- <xref:Rowles.LeanLucene.Search.Queries.VectorQuery>
