using System.Buffers;
using System.IO.Compression;
using NativeCompressions;

namespace Rowles.LeanLucene.Codecs.StoredFields;

/// <summary>Compression/decompression dispatch for stored field blocks.</summary>
internal static class StoredFieldCompression
{
    /// <summary>Compresses raw block data using the specified policy.</summary>
    internal static (byte[] Data, int Length) Compress(ReadOnlySpan<byte> raw, FieldCompressionPolicy policy)
    {
        if (policy == FieldCompressionPolicy.None || raw.Length == 0)
        {
            var copy = new byte[raw.Length];
            raw.CopyTo(copy);
            return (copy, raw.Length);
        }

        if (policy == FieldCompressionPolicy.Lz4)
        {
            byte[] compressed = LZ4.Compress(raw);
            return (compressed, compressed.Length);
        }

        if (policy == FieldCompressionPolicy.Zstandard)
        {
            byte[] compressed = Zstandard.Compress(raw);
            return (compressed, compressed.Length);
        }

        // Brotli fallback (shouldn't be used for writing)
        using var ms = new MemoryStream();
        using (var brotli = new BrotliStream(ms, CompressionLevel.Fastest, leaveOpen: true))
            brotli.Write(raw);
        return (ms.ToArray(), (int)ms.Length);
    }

    /// <summary>Decompresses block data using the specified policy.</summary>
    internal static byte[] Decompress(ReadOnlySpan<byte> compressed, int originalSize, FieldCompressionPolicy policy)
    {
        if (policy == FieldCompressionPolicy.None || compressed.Length == originalSize)
        {
            var raw = new byte[originalSize];
            compressed[..originalSize].CopyTo(raw);
            return raw;
        }

        if (policy == FieldCompressionPolicy.Lz4)
            return LZ4.Decompress(compressed, trustedData: true);

        if (policy == FieldCompressionPolicy.Zstandard)
            return Zstandard.Decompress(compressed, trustedData: true);

        // Brotli (legacy v4 segments)
        using var compStream = new MemoryStream(compressed.ToArray());
        using var brotli = new BrotliStream(compStream, CompressionMode.Decompress);
        var raw2 = new byte[originalSize];
        int totalRead = 0;
        while (totalRead < originalSize)
        {
            int read = brotli.Read(raw2, totalRead, originalSize - totalRead);
            if (read == 0) break;
            totalRead += read;
        }
        return raw2;
    }
}
