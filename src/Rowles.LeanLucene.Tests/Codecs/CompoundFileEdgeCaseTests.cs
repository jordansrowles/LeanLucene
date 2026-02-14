using Rowles.LeanLucene.Codecs;
using Rowles.LeanLucene.Tests.Fixtures;
using Xunit.Abstractions;

namespace Rowles.LeanLucene.Tests.Codecs;

/// <summary>
/// Tests the CompoundFileWriter/CompoundFileReader round-trip and edge cases.
/// The search path does not yet consume .cfs files, so we test the codec directly.
/// </summary>
[Trait("Category", "Codecs")]
public sealed class CompoundFileEdgeCaseTests : IClassFixture<TestDirectoryFixture>
{
    private readonly TestDirectoryFixture _fixture;
    private readonly ITestOutputHelper _output;

    public CompoundFileEdgeCaseTests(TestDirectoryFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    private string SubDir(string name)
    {
        var path = Path.Combine(_fixture.Path, name);
        Directory.CreateDirectory(path);
        return path;
    }

    // ── Round-trip: write files then read back from .cfs ────────────────────

    [Fact]
    public void RoundTrip_MultipleFiles_DataPreserved()
    {
        // Arrange
        var dir = SubDir("cfs_roundtrip");
        var basePath = Path.Combine(dir, "seg0");
        var content1 = new byte[] { 1, 2, 3, 4, 5 };
        var content2 = new byte[] { 10, 20, 30, 40, 50, 60, 70, 80 };
        File.WriteAllBytes(basePath + ".dic", content1);
        File.WriteAllBytes(basePath + ".pos", content2);

        // Act
        CompoundFileWriter.Write(basePath + ".cfs", basePath, [".dic", ".pos"]);

        // Assert
        using var reader = CompoundFileReader.Open(basePath + ".cfs");
        Assert.True(reader.HasFile(".dic"));
        Assert.True(reader.HasFile(".pos"));
        Assert.Equal(content1, reader.ReadFile(".dic"));
        Assert.Equal(content2, reader.ReadFile(".pos"));
        _output.WriteLine("✓ Round-trip preserves data for multiple files");
    }

    // ── Single file compound ────────────────────────────────────────────────

    [Fact]
    public void SingleFile_RoundTrips()
    {
        // Arrange
        var dir = SubDir("cfs_single");
        var basePath = Path.Combine(dir, "seg0");
        var content = new byte[1024];
        Random.Shared.NextBytes(content);
        File.WriteAllBytes(basePath + ".nrm", content);

        // Act
        CompoundFileWriter.Write(basePath + ".cfs", basePath, [".nrm"]);

        // Assert
        using var reader = CompoundFileReader.Open(basePath + ".cfs");
        Assert.True(reader.HasFile(".nrm"));
        Assert.False(reader.HasFile(".dic"));
        Assert.Equal(content, reader.ReadFile(".nrm"));
        _output.WriteLine("✓ Single-file compound round-trips correctly");
    }

    // ── Missing extension throws FileNotFoundException ───────────────────────

    [Fact]
    public void ReadMissingExtension_Throws()
    {
        // Arrange
        var dir = SubDir("cfs_missing_ext");
        var basePath = Path.Combine(dir, "seg0");
        File.WriteAllBytes(basePath + ".dic", [0xAB]);
        CompoundFileWriter.Write(basePath + ".cfs", basePath, [".dic"]);

        // Act & Assert
        using var reader = CompoundFileReader.Open(basePath + ".cfs");
        Assert.Throws<FileNotFoundException>(() => reader.ReadFile(".pos"));
        _output.WriteLine("✓ Reading absent extension throws FileNotFoundException");
    }

    // ── ListFiles enumerates all entries ─────────────────────────────────────

    [Fact]
    public void ListFiles_ReturnsAllExtensions()
    {
        // Arrange
        var dir = SubDir("cfs_list");
        var basePath = Path.Combine(dir, "seg0");
        string[] exts = [".dic", ".pos", ".nrm"];
        foreach (var ext in exts)
            File.WriteAllBytes(basePath + ext, [0x01]);
        CompoundFileWriter.Write(basePath + ".cfs", basePath, exts);

        // Act
        using var reader = CompoundFileReader.Open(basePath + ".cfs");
        var listed = reader.ListFiles().OrderBy(f => f).ToArray();

        // Assert
        Assert.Equal(exts.OrderBy(e => e).ToArray(), listed);
        _output.WriteLine($"✓ ListFiles returned: {string.Join(", ", listed)}");
    }
}
