using Rowles.LeanLucene.Codecs.DocValues;

namespace Rowles.LeanLucene.Tests.Codecs;

/// <summary>
/// Contains unit tests for binary DocValues.
/// </summary>
[Trait("Category", "Codecs")]
public sealed class BinaryDocValuesTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), $"ll-dvb-{Guid.NewGuid():N}");

    public BinaryDocValuesTests() => Directory.CreateDirectory(_dir);

    public void Dispose()
    {
        if (Directory.Exists(_dir))
            Directory.Delete(_dir, recursive: true);
    }

    /// <summary>
    /// Verifies repeated UTF-8 values and empty strings round-trip in order.
    /// </summary>
    [Fact(DisplayName = "Roundtrip: Repeated Values Preserve Order")]
    public void Roundtrip_RepeatedValues_PreserveOrder()
    {
        var path = Path.Combine(_dir, "stored.dvb");
        static byte[] Bytes(string value) => System.Text.Encoding.UTF8.GetBytes(value);

        IReadOnlyList<byte[]>?[] values =
        [
            [Bytes("alpha"), Bytes(""), Bytes("bravo")],
            null,
            [Bytes("cafe")]
        ];
        var fields = new Dictionary<string, IReadOnlyList<byte[]>?[]>
        {
            ["stored"] = values
        };

        BinaryDocValuesWriter.Write(path, fields, 3);
        var result = BinaryDocValuesReader.Read(path);

        Assert.Equal(["alpha", "", "bravo"], result["stored"][0].Select(static value => System.Text.Encoding.UTF8.GetString(value)));
        Assert.Empty(result["stored"][1]);
        Assert.Equal(["cafe"], result["stored"][2].Select(static value => System.Text.Encoding.UTF8.GetString(value)));
    }

    /// <summary>
    /// Verifies missing optional sidecar files are treated as empty.
    /// </summary>
    [Fact(DisplayName = "Read: Missing File Returns Empty")]
    public void Read_MissingFile_ReturnsEmpty()
    {
        var result = BinaryDocValuesReader.Read(Path.Combine(_dir, "missing.dvb"));
        Assert.Empty(result);
    }
}
