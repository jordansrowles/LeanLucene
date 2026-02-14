using System.IO.Compression;

namespace Rowles.LeanLucene.Codecs.StoredFields;

/// <summary>
/// Simple compression policy for stored fields.
/// Maps to the underlying Brotli compression level.
/// </summary>
public enum FieldCompressionPolicy
{
    /// <summary>No compression. Fastest writes, largest on-disc size.</summary>
    None,

    /// <summary>Fast compression (Brotli Fastest). Good balance of speed and size.</summary>
    Fast,

    /// <summary>High compression (Brotli Optimal). Smaller files, slower writes.</summary>
    High
}
