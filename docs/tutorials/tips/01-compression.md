# Stored field compression

Stored fields are written in blocks under the
`.fdt` extension and compressed.

## Choose a policy

```csharp
var config = new IndexWriterConfig
{
    CompressionPolicy = FieldCompressionPolicy.Lz4, // default
    StoredFieldBlockSize = 16,                       // docs per block
};
```

| Policy | Notes |
|---|---|
| `None` | No compression. Fastest write, largest disk. |
| `Lz4` (default) | Very fast, modest ratio. |
| `Zstandard` | Better ratio than LZ4, still fast. |

The policy is recorded in the segment header, so reads tolerate mixed segments.

## Block size

`StoredFieldBlockSize` (default `16`) controls how many documents share a
compression block. Larger blocks compress better but cost more on single-document
retrieval.

## Trade-offs

- Indexing throughput: `None` > `Lz4` > `Zstandard`.
- On-disk size: `Zstandard` < `Lz4` < `None`.
- Retrieval cost scales with block size, not policy.

## See also

- <xref:Rowles.LeanLucene.Codecs.StoredFields.FieldCompressionPolicy>
- <xref:Rowles.LeanLucene.Index.Indexer.IndexWriterConfig>
