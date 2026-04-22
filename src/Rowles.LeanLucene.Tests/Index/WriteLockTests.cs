using System.Collections.Concurrent;
using Rowles.LeanLucene.Document;
using Rowles.LeanLucene.Document.Fields;
using Rowles.LeanLucene.Index;
using Rowles.LeanLucene.Store;
using Rowles.LeanLucene.Tests.Fixtures;

namespace Rowles.LeanLucene.Tests.Index;

[Trait("Category", "Index")]
[Trait("Category", "WriteLock")]
public sealed class WriteLockTests : IClassFixture<TestDirectoryFixture>
{
    private readonly TestDirectoryFixture _fixture;
    public WriteLockTests(TestDirectoryFixture fixture) => _fixture = fixture;

    private string SubDir(string name)
    {
        var path = System.IO.Path.Combine(_fixture.Path, name);
        System.IO.Directory.CreateDirectory(path);
        return path;
    }

    [Fact]
    public void WriteLock_AcquiredOnConstruction()
    {
        var dir = new MMapDirectory(SubDir("lock_acquired"));
        using var writer = new IndexWriter(dir, new IndexWriterConfig());
        Assert.True(File.Exists(Path.Combine(dir.DirectoryPath, "write.lock")));
    }

    [Fact]
    public void WriteLock_ReleasedOnDispose()
    {
        var dir = new MMapDirectory(SubDir("lock_released"));
        using (var writer = new IndexWriter(dir, new IndexWriterConfig()))
        {
            // lock held
        }
        // After dispose: lock file should be removed
        Assert.False(File.Exists(Path.Combine(dir.DirectoryPath, "write.lock")));
    }

    [Fact]
    public void WriteLock_SecondWriter_SameDirectory_ThrowsWriteLockException()
    {
        var dir = new MMapDirectory(SubDir("lock_conflict"));
        using var writer1 = new IndexWriter(dir, new IndexWriterConfig());

        Assert.Throws<WriteLockException>(() =>
            new IndexWriter(dir, new IndexWriterConfig()));
    }

    [Fact]
    public void WriteLock_FailedSecondWriter_DoesNotReleaseFirstWriterLock()
    {
        var dir = new MMapDirectory(SubDir("lock_failed_second_writer"));
        using var writer1 = new IndexWriter(dir, new IndexWriterConfig());

        Assert.Throws<WriteLockException>(() => new IndexWriter(dir, new IndexWriterConfig()));

        var doc = new LeanDocument();
        doc.Add(new TextField("body", "first writer still owns lock"));
        writer1.AddDocument(doc);
        writer1.Commit();

        Assert.Throws<WriteLockException>(() => new IndexWriter(dir, new IndexWriterConfig()));
    }

    [Fact]
    public void WriteLock_StaleLockFileWithoutHandle_DoesNotBlockWriter()
    {
        var dir = new MMapDirectory(SubDir("lock_stale_file"));
        File.WriteAllText(Path.Combine(dir.DirectoryPath, "write.lock"), "stale");

        using var writer = new IndexWriter(dir, new IndexWriterConfig());
        var doc = new LeanDocument();
        doc.Add(new TextField("body", "stale lock ignored"));
        writer.AddDocument(doc);
        writer.Commit();

        using var searcher = new IndexSearcher(dir);
        Assert.Equal(1, searcher.Search(new TermQuery("body", "stale"), 10).TotalHits);
    }

    [Fact(Timeout = 10_000)]
    public async Task WriteLock_ConcurrentWriterConstruction_AllowsExactlyOneWriter()
    {
        var dir = new MMapDirectory(SubDir("lock_concurrent_construction"));
        var successes = new ConcurrentBag<IndexWriter>();
        var failures = new ConcurrentBag<Exception>();

        var tasks = Enumerable.Range(0, 16)
            .Select(_ => Task.Run(() =>
            {
                try
                {
                    successes.Add(new IndexWriter(dir, new IndexWriterConfig()));
                }
                catch (Exception ex)
                {
                    failures.Add(ex);
                }
            }))
            .ToArray();

        await Task.WhenAll(tasks);

        try
        {
            Assert.Single(successes);
            Assert.Equal(15, failures.Count);
            Assert.All(failures, failure => Assert.IsType<WriteLockException>(failure));
        }
        finally
        {
            foreach (var writer in successes)
                writer.Dispose();
        }
    }

    [Fact]
    public void WriteLock_AfterFirstWriterDisposed_SecondWriterSucceeds()
    {
        var dir = new MMapDirectory(SubDir("lock_reacquire"));
        using (var writer1 = new IndexWriter(dir, new IndexWriterConfig()))
        {
            var doc = new LeanDocument();
            doc.Add(new TextField("body", "hello"));
            writer1.AddDocument(doc);
            writer1.Commit();
        }

        // Should not throw — first writer released the lock
        using var writer2 = new IndexWriter(dir, new IndexWriterConfig());
        Assert.NotNull(writer2);
    }
}
