using System.IO.Compression;

namespace Rowles.LeanLucene.Codecs.StoredFields;

/// <summary>Extension methods for <see cref="FieldCompressionPolicy"/>.</summary>
internal static class FieldCompressionPolicyExtensions
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
