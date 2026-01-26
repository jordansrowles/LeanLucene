using System.IO.Compression;

namespace Rowles.LeanLucene.Codecs;

/// <summary>
/// Simple compression policy for stored fields.
/// Maps to the underlying Brotli compression level.
/// </summary>
public enum FieldCompressionPolicy
{
    /// <summary>No compression. Fastest writes, largest on-disk size.</summary>
    None,

    /// <summary>Fast compression (Brotli Fastest). Good balance of speed and size.</summary>
    Fast,

    /// <summary>High compression (Brotli Optimal). Smaller files, slower writes.</summary>
    High
}

/// <summary>Extension methods for <see cref="FieldCompressionPolicy"/>.</summary>
public static class FieldCompressionPolicyExtensions
{
    /// <summary>Maps the policy to the corresponding <see cref="CompressionLevel"/>.</summary>
    public static CompressionLevel ToCompressionLevel(this FieldCompressionPolicy policy) => policy switch
    {
        FieldCompressionPolicy.None => CompressionLevel.NoCompression,
        FieldCompressionPolicy.Fast => CompressionLevel.Fastest,
        FieldCompressionPolicy.High => CompressionLevel.Optimal,
        _ => CompressionLevel.Fastest
    };
}
