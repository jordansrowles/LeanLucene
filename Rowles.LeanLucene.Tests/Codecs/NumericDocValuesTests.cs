using Rowles.LeanLucene.Codecs;
using Rowles.LeanLucene.Store;

namespace Rowles.LeanLucene.Tests.Codecs;

public sealed class NumericDocValuesTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), $"ll-ndv-{Guid.NewGuid():N}");

    public NumericDocValuesTests() => Directory.CreateDirectory(_dir);

    public void Dispose()
    {
        if (Directory.Exists(_dir))
            Directory.Delete(_dir, recursive: true);
    }

    [Fact]
    public void Roundtrip_SingleField_PreservesValues()
    {
        var path = Path.Combine(_dir, "test.dvn");
        var fields = new Dictionary<string, double[]>
        {
            ["price"] = [1.99, 2.50, 3.75, 0.0, 100.0]
        };

        NumericDocValuesWriter.Write(path, fields, 5);
        var result = NumericDocValuesReader.Read(path);

        Assert.Single(result);
        Assert.True(result.ContainsKey("price"));
        Assert.Equal(fields["price"], result["price"]);
    }

    [Fact]
    public void Roundtrip_MultipleFields_PreservesAll()
    {
        var path = Path.Combine(_dir, "multi.dvn");
        var fields = new Dictionary<string, double[]>
        {
            ["price"] = [10.0, 20.0, 30.0],
            ["rating"] = [4.5, 3.2, 5.0]
        };

        NumericDocValuesWriter.Write(path, fields, 3);
        var result = NumericDocValuesReader.Read(path);

        Assert.Equal(2, result.Count);
        Assert.Equal(fields["price"], result["price"]);
        Assert.Equal(fields["rating"], result["rating"]);
    }

    [Fact]
    public void Roundtrip_AllSameValue_UsesZeroBits()
    {
        var path = Path.Combine(_dir, "const.dvn");
        var fields = new Dictionary<string, double[]>
        {
            ["score"] = [42.0, 42.0, 42.0, 42.0]
        };

        NumericDocValuesWriter.Write(path, fields, 4);
        var result = NumericDocValuesReader.Read(path);

        Assert.Equal(fields["score"], result["score"]);
    }

    [Fact]
    public void Read_MissingFile_ReturnsEmpty()
    {
        var result = NumericDocValuesReader.Read(Path.Combine(_dir, "nonexistent.dvn"));
        Assert.Empty(result);
    }
}
