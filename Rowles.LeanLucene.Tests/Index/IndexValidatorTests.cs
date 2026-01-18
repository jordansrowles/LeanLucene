using Rowles.LeanLucene.Document;
using Rowles.LeanLucene.Index;
using Rowles.LeanLucene.Store;
using Rowles.LeanLucene.Tests.Fixtures;

namespace Rowles.LeanLucene.Tests.Index;

[Trait("Category", "Index")]
[Trait("Category", "Validation")]
public sealed class IndexValidatorTests : IClassFixture<TestDirectoryFixture>
{
    private readonly TestDirectoryFixture _fixture;
    public IndexValidatorTests(TestDirectoryFixture fixture) => _fixture = fixture;

    private string SubDir(string name)
    {
        var path = System.IO.Path.Combine(_fixture.Path, name);
        System.IO.Directory.CreateDirectory(path);
        return path;
    }

    [Fact]
    public void Validate_EmptyDirectory_ReportsNoCommitFile()
    {
        var dir = new MMapDirectory(SubDir("val_empty"));
        var result = IndexValidator.Validate(dir);
        Assert.False(result.IsHealthy);
        Assert.Single(result.Issues);
    }

    [Fact]
    public void Validate_ValidIndex_IsHealthy()
    {
        var dir = new MMapDirectory(SubDir("val_valid"));
        using var writer = new IndexWriter(dir, new IndexWriterConfig());
        var doc = new LeanDocument();
        doc.Add(new TextField("body", "hello world"));
        doc.Add(new StringField("id", "1"));
        writer.AddDocument(doc);
        writer.Commit();

        var result = IndexValidator.Validate(dir);
        Assert.True(result.IsHealthy, string.Join(", ", result.Issues));
        Assert.Equal(1, result.SegmentsChecked);
        Assert.True(result.DocumentsChecked >= 1);
    }

    [Fact]
    public void Validate_MultipleSegments_ChecksAll()
    {
        var dir = new MMapDirectory(SubDir("val_multiseg"));
        using var writer = new IndexWriter(dir, new IndexWriterConfig());

        for (int i = 0; i < 3; i++)
        {
            var doc = new LeanDocument();
            doc.Add(new TextField("body", $"document {i}"));
            writer.AddDocument(doc);
            writer.Commit();
        }

        var result = IndexValidator.Validate(dir);
        Assert.True(result.IsHealthy, string.Join(", ", result.Issues));
    }

    [Fact]
    public void Validate_MissingSegmentFile_ReportsIssue()
    {
        var dir = new MMapDirectory(SubDir("val_missing"));
        using var writer = new IndexWriter(dir, new IndexWriterConfig());
        var doc = new LeanDocument();
        doc.Add(new TextField("body", "hello"));
        writer.AddDocument(doc);
        writer.Commit();

        // Delete one of the required segment files
        var dicFiles = Directory.GetFiles(dir.DirectoryPath, "*.dic");
        if (dicFiles.Length > 0)
            File.Delete(dicFiles[0]);

        var result = IndexValidator.Validate(dir);
        Assert.False(result.IsHealthy);
    }
}
