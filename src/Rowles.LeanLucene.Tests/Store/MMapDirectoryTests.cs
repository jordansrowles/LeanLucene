using Rowles.LeanLucene.Store;
using Rowles.LeanLucene.Tests.Fixtures;

namespace Rowles.LeanLucene.Tests.Store;

/// <summary>
/// Contains unit tests for MMap Directory.
/// </summary>
[Trait("Category", "Store")]
public sealed class MMapDirectoryTests : IClassFixture<TestDirectoryFixture>
{
    private readonly TestDirectoryFixture _fixture;

    public MMapDirectoryTests(TestDirectoryFixture fixture) => _fixture = fixture;

    /// <summary>
    /// Verifies the MMap Directory: Open Input Returns Readable Span scenario.
    /// </summary>
    [Fact(DisplayName = "MMap Directory: Open Input Returns Readable Span")]
    public void MMapDirectory_OpenInput_ReturnsReadableSpan()
    {
        // Write known bytes to a file, open via MMapDirectory,
        // assert the returned IndexInput span contains the exact bytes.
        var dir = new MMapDirectory(_fixture.Path);
        var fileName = "test_readable.bin";
        var expected = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF, 0x01, 0x02, 0x03, 0x04 };

        using (var output = dir.CreateOutput(fileName))
        {
            output.WriteBytes(expected);
        }

        using var input = dir.OpenInput(fileName);
        var actual = input.ReadBytes(expected.Length);
        Assert.Equal(expected, actual);
    }

    /// <summary>
    /// Verifies the Index Input: Read Unaligned Returns Correct Primitive scenario.
    /// </summary>
    [Fact(DisplayName = "Index Input: Read Unaligned Returns Correct Primitive")]
    public void IndexInput_ReadUnaligned_ReturnsCorrectPrimitive()
    {
        // Write a float and an int at non-aligned offsets, read back via
        // Unsafe.ReadUnaligned<T>, assert values match.
        var dir = new MMapDirectory(_fixture.Path);
        var fileName = "test_unaligned.bin";
        float expectedFloat = 3.14f;
        int expectedInt = 42;

        using (var output = dir.CreateOutput(fileName))
        {
            output.WriteByte(0xFF); // padding byte to create misalignment
            output.WriteInt32(expectedInt);
            output.WriteSingle(expectedFloat);
        }

        using var input = dir.OpenInput(fileName);
        input.Seek(1); // skip padding
        var actualInt = input.ReadInt32();
        var actualFloat = input.ReadSingle();

        Assert.Equal(expectedInt, actualInt);
        Assert.Equal(expectedFloat, actualFloat);
    }

    /// <summary>
    /// Verifies the Index Output: Write And Flush Produces Durable File scenario.
    /// </summary>
    [Fact(DisplayName = "Index Output: Write And Flush Produces Durable File")]
    public void IndexOutput_WriteAndFlush_ProducesDurableFile()
    {
        // Write a sequence of bytes via IndexOutput, dispose, read back raw,
        // assert byte-for-byte equality.
        var dir = new MMapDirectory(_fixture.Path);
        var fileName = "test_durable.bin";
        var expected = new byte[256];
        for (int i = 0; i < expected.Length; i++)
            expected[i] = (byte)(i & 0xFF);

        using (var output = dir.CreateOutput(fileName))
        {
            output.WriteBytes(expected);
        }

        var filePath = System.IO.Path.Combine(_fixture.Path, fileName);
        var actual = File.ReadAllBytes(filePath);
        Assert.Equal(expected, actual);
    }

    /// <summary>
    /// Verifies the Index Input: Read Int 32 Beyond File End Throws End Of Stream Exception scenario.
    /// </summary>
    [Fact(DisplayName = "Index Input: Read Int 32 Beyond File End Throws End Of Stream Exception")]
    public void IndexInput_ReadInt32_BeyondFileEnd_ThrowsEndOfStreamException()
    {
        var dir = new MMapDirectory(_fixture.Path);
        var fileName = "test_bounds.bin";

        using (var output = dir.CreateOutput(fileName))
        {
            output.WriteByte(0xAA);
        }

        using var input = dir.OpenInput(fileName);
        Assert.Throws<EndOfStreamException>(() => input.ReadInt32());
    }

    /// <summary>
    /// Verifies the MMap Directory: Path Traversal File Name Throws Argument Exception scenario.
    /// </summary>
    [Fact(DisplayName = "MMap Directory: Path Traversal File Name Throws Argument Exception")]
    public void MMapDirectory_PathTraversalFileName_ThrowsArgumentException()
    {
        var dir = new MMapDirectory(_fixture.Path);
        Assert.Throws<ArgumentException>(() => dir.OpenInput("..\\..\\windows\\win.ini"));
    }

    /// <summary>
    /// Verifies the Index Output: Use Pooled Buffer No Heap Allocation scenario.
    /// </summary>
    [Fact(DisplayName = "Index Output: Use Pooled Buffer No Heap Allocation")]
    [Trait("Category", "Advanced")]
    public void IndexOutput_UsePooledBuffer_NoHeapAllocation()
    {
        // Write 10 MB through IndexOutput, assert GC allocation delta
        // is below a defined threshold.
        var dir = new MMapDirectory(_fixture.Path);
        var fileName = "test_pooled.bin";
        var data = new byte[1024];
        Array.Fill<byte>(data, 0xAB);

        // Warm up
        using (var warmup = dir.CreateOutput("warmup.bin"))
        {
            warmup.WriteBytes(data);
        }

        long before = GC.GetAllocatedBytesForCurrentThread();

        using (var output = dir.CreateOutput(fileName))
        {
            for (int i = 0; i < 10_240; i++)
                output.WriteBytes(data);
        }

        long after = GC.GetAllocatedBytesForCurrentThread();
        long allocated = after - before;

        Assert.True(allocated < 100_000,
            $"Expected fewer than 100 KB allocated, but got {allocated / 1024.0:F1} KB");
    }
}
