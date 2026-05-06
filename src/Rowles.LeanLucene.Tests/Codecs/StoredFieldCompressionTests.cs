using Rowles.LeanLucene.Codecs.StoredFields;

namespace Rowles.LeanLucene.Tests.Codecs;

/// <summary>
/// Unit tests for the internal <see cref="StoredFieldCompression"/> helper covering
/// all four <see cref="FieldCompressionPolicy"/> branches in both Compress and
/// Decompress, including round-trip correctness and empty-input handling.
/// </summary>
[Trait("Category", "Codecs")]
[Trait("Category", "UnitTest")]
public sealed class StoredFieldCompressionTests
{
    private static readonly byte[] SampleData =
        System.Text.Encoding.UTF8.GetBytes("The quick brown fox jumps over the lazy dog.");

    // ── None ──────────────────────────────────────────────────────────────────

    [Fact(DisplayName = "StoredFieldCompression: None Policy Round-Trips")]
    public void None_RoundTrips()
    {
        var (compressed, length) = StoredFieldCompression.Compress(SampleData, FieldCompressionPolicy.None);
        var restored = StoredFieldCompression.Decompress(compressed, length, FieldCompressionPolicy.None);
        Assert.Equal(SampleData, restored);
    }

    [Fact(DisplayName = "StoredFieldCompression: None Policy Is A Copy")]
    public void None_IsCopy_NotSameInstance()
    {
        var (compressed, _) = StoredFieldCompression.Compress(SampleData, FieldCompressionPolicy.None);
        Assert.NotSame(SampleData, compressed);
        Assert.Equal(SampleData, compressed);
    }

    [Fact(DisplayName = "StoredFieldCompression: None Policy With Empty Input")]
    public void None_EmptyInput_RoundTrips()
    {
        var (compressed, length) = StoredFieldCompression.Compress([], FieldCompressionPolicy.None);
        Assert.Equal(0, length);
        var restored = StoredFieldCompression.Decompress(compressed, 0, FieldCompressionPolicy.None);
        Assert.Empty(restored);
    }

    // ── Lz4 ───────────────────────────────────────────────────────────────────

    [Fact(DisplayName = "StoredFieldCompression: Lz4 Policy Round-Trips")]
    public void Lz4_RoundTrips()
    {
        var (compressed, length) = StoredFieldCompression.Compress(SampleData, FieldCompressionPolicy.Lz4);
        var restored = StoredFieldCompression.Decompress(compressed.AsSpan()[..length], SampleData.Length, FieldCompressionPolicy.Lz4);
        Assert.Equal(SampleData, restored);
    }

    // ── Zstandard ─────────────────────────────────────────────────────────────

    [Fact(DisplayName = "StoredFieldCompression: Zstandard Policy Round-Trips")]
    public void Zstandard_RoundTrips()
    {
        var (compressed, length) = StoredFieldCompression.Compress(SampleData, FieldCompressionPolicy.Zstandard);
        var restored = StoredFieldCompression.Decompress(compressed.AsSpan()[..length], SampleData.Length, FieldCompressionPolicy.Zstandard);
        Assert.Equal(SampleData, restored);
    }

    // ── Brotli (legacy) ───────────────────────────────────────────────────────

    [Fact(DisplayName = "StoredFieldCompression: Brotli Policy Round-Trips")]
    public void Brotli_RoundTrips()
    {
        var (compressed, length) = StoredFieldCompression.Compress(SampleData, FieldCompressionPolicy.Brotli);
        var restored = StoredFieldCompression.Decompress(compressed.AsSpan()[..length], SampleData.Length, FieldCompressionPolicy.Brotli);
        Assert.Equal(SampleData, restored);
    }

    // ── Decompress: None when compressed.Length == originalSize ──────────────

    [Fact(DisplayName = "StoredFieldCompression: Decompress None By Size Equality")]
    public void Decompress_NoneBySizeEquality_RoundTrips()
    {
        // When compressed.Length == originalSize, Decompress short-circuits to the None branch
        // regardless of the policy enum value.
        var raw = new byte[] { 1, 2, 3, 4, 5 };
        var restored = StoredFieldCompression.Decompress(raw, raw.Length, FieldCompressionPolicy.Lz4);
        Assert.Equal(raw, restored);
    }
}
