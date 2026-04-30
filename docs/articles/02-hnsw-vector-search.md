# HNSW vector search

Vector fields are stored per segment and now get an HNSW graph at flush time by
default. `VectorQuery` uses the graph when it is present, then reranks the
shortlist with exact cosine similarity.

The important knobs are:

| Setting | Use |
|---|---|
| `BuildHnswOnFlush` | Disable only for tiny indices or controlled comparisons |
| `NormaliseVectors` | Keep on for cosine search unless vectors are already prepared |
| `HnswBuildConfig.M` | Higher values improve recall and increase index size |
| `VectorQuery.EfSearch` | Higher values improve recall and increase latency |
| `OversamplingFactor` | Rerank a larger shortlist for better final ordering |

Filters are handled by selectivity. Very tight filters scan the matched docs.
Medium filters use an allow-list during graph traversal. Loose filters use
post-filtering with retries.

See [Vector search](../tutorials/advanced/05-vector-search.md).
