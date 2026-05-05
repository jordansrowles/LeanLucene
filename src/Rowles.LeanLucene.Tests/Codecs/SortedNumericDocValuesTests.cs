using Rowles.LeanLucene.Codecs.DocValues;

namespace Rowles.LeanLucene.Tests.Codecs;

/// <summary>
/// Contains unit tests for sorted-numeric DocValues.
/// </summary>
[Trait("Category", "Codecs")]
public sealed class SortedNumericDocValuesTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), $"ll-dsn-{Guid.NewGuid():N}");

    public SortedNumericDocValuesTests() => Directory.CreateDirectory(_dir);

    public void Dispose()
    {
        if (Directory.Exists(_dir))
            Directory.Delete(_dir, recursive: true);
    }

    /// <summary>
    /// Verifies repeated numeric values round-trip sorted per document.
    /// </summary>
    [Fact(DisplayName = "Roundtrip: Repeated Numeric Values Are Sorted")]
    public void Roundtrip_RepeatedNumericValues_AreSorted()
    {
        var path = Path.Combine(_dir, "scores.dsn");
        IReadOnlyList<double>?[] scores =
        [
            [10, -2, 0],
            null,
            [3.5, 3.5, -7]
        ];
        var fields = new Dictionary<string, IReadOnlyList<double>?[]>
        {
            ["score"] = scores
        };

        SortedNumericDocValuesWriter.Write(path, fields, 3);
        var result = SortedNumericDocValuesReader.Read(path);

        Assert.Equal([-2, 0, 10], result["score"][0]);
        Assert.Empty(result["score"][1]);
        Assert.Equal([-7, 3.5, 3.5], result["score"][2]);
    }

    /// <summary>
    /// Verifies missing optional sidecar files are treated as empty.
    /// </summary>
    [Fact(DisplayName = "Read: Missing File Returns Empty")]
    public void Read_MissingFile_ReturnsEmpty()
    {
        var result = SortedNumericDocValuesReader.Read(Path.Combine(_dir, "missing.dsn"));
        Assert.Empty(result);
    }
}
