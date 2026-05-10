using System.Text;
using Rowles.LeanLucene.Codecs;
using Rowles.LeanLucene.Codecs.TermDictionary;
using Rowles.LeanLucene.Document;
using Rowles.LeanLucene.Document.Fields;
using Rowles.LeanLucene.Index.Compatibility;
using Rowles.LeanLucene.Index.Migration;
using Rowles.LeanLucene.Store;
using Rowles.LeanLucene.Tests.Fixtures;

namespace Rowles.LeanLucene.Tests.Index;

[Trait("Category", "Index")]
[Trait("Category", "Migration")]
public sealed class IndexCodecMigratorTests : IClassFixture<TestDirectoryFixture>
{
    private readonly TestDirectoryFixture _fixture;

    public IndexCodecMigratorTests(TestDirectoryFixture fixture) => _fixture = fixture;

    [Fact]
    public void Migrate_OlderTermDictionary_StagesAndPublishesCurrentCodec()
    {
        using var directory = CreateIndex("migration_dic");
        RewriteTermDictionaryAsV1(Directory.GetFiles(directory.DirectoryPath, "*.dic").Single());
        var stagingDirectory = Path.Combine(_fixture.Path, "migration_dic_staging");

        var result = IndexCodecMigrator.Migrate(directory, new IndexCodecMigrationOptions
        {
            DryRun = false,
            StagingDirectory = stagingDirectory
        });

        Assert.True(result.Succeeded, string.Join(Environment.NewLine, result.Issues.Select(static issue => issue.ToString())));
        Assert.Equal(IndexMigrationState.Published, IndexMigrationRecovery.GetState(directory.DirectoryPath).State);
        Assert.Equal(IndexCompatibilityStatus.Compatible, IndexCompatibility.Check(directory).Status);
    }

    private MMapDirectory CreateIndex(string name)
    {
        var path = Path.Combine(_fixture.Path, name);
        Directory.CreateDirectory(path);
        var directory = new MMapDirectory(path);
        using var writer = new IndexWriter(directory, new IndexWriterConfig());
        var document = new LeanDocument();
        document.Add(new TextField("body", "hello world"));
        document.Add(new StringField("id", "1"));
        writer.AddDocument(document);
        writer.Commit();
        return directory;
    }

    private static void RewriteTermDictionaryAsV1(string path)
    {
        using var reader = TermDictionaryReader.Open(path);
        var terms = reader.EnumerateAllTerms();

        using var stream = File.Create(path);
        using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: false);
        CodecConstants.WriteHeader(writer, 1);
        writer.Write(0);
        foreach (var (term, offset) in terms)
        {
            var bytes = Encoding.UTF8.GetBytes(term);
            writer.Write(bytes.Length);
            writer.Write(bytes);
            writer.Write(offset);
        }
    }
}
