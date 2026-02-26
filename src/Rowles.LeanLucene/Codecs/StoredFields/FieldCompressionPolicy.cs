namespace Rowles.LeanLucene.Codecs.StoredFields;

/// <summary>
/// Compression algorithm for stored fields.
/// </summary>
public enum FieldCompressionPolicy : byte
{
    /// <summary>No compression. Fastest writes, largest on-disc size.</summary>
    None = 0,

    /// <summary>LZ4 compression. Extremely fast decompression (~3 GB/s).</summary>
    Lz4 = 1,

    /// <summary>Zstandard compression. Better ratio than LZ4, still very fast.</summary>
    Zstandard = 2,

    /// <summary>Brotli compression (legacy, for reading old segments only).</summary>
    Brotli = 3
}
