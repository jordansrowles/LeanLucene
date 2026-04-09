using Rowles.LeanLucene.Document;
using Rowles.LeanLucene.Document.Fields;
using Rowles.LeanLucene.Index;
using Rowles.LeanLucene.Search;
using Rowles.LeanLucene.Store;

namespace Rowles.LeanLucene.Tests.Index;

[Trait("Category", "ThreadSafety")]
public sealed class SegmentReaderThreadSafetyTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), $"ll-reader-ts-{Guid.NewGuid():N}");

    public SegmentReaderThreadSafetyTests() => Directory.CreateDirectory(_dir);

    public void Dispose()
    {
        if (Directory.Exists(_dir))
            Directory.Delete(_dir, recursive: true);
    }

    [Fact]
    public void ConcurrentTermLookup_DoesNotCorruptCache()
    {
        const int docCount = 150;
        const int threadCount = 16;
        const int itersPerThread = 40;

        var directory = new MMapDirectory(_dir);
        using var writer = new IndexWriter(directory, new IndexWriterConfig { MaxBufferedDocs = 500 });

        for (int i = 0; i < docCount; i++)
        {
            var doc = new LeanDocument();
            doc.Add(new TextField("body", $"alpha{i % 10} beta{i % 5} gamma"));
            writer.AddDocument(doc);
        }
        writer.Commit();

        // Open a single IndexSearcher and drive concurrent queries so they share a SegmentReader
        using var searcher = new IndexSearcher(directory);

        // Establish reference counts for each term
        var referenceDocFreqs = new Dictionary<string, int>();
        for (int t = 0; t < 10; t++)
            referenceDocFreqs[$"alpha{t}"] = searcher.Search(new TermQuery("body", $"alpha{t}"), docCount).TotalHits;
        for (int t = 0; t < 5; t++)
            referenceDocFreqs[$"beta{t}"] = searcher.Search(new TermQuery("body", $"beta{t}"), docCount).TotalHits;

        var errors = new System.Collections.Concurrent.ConcurrentBag<string>();
        var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();

        var threads = Enumerable.Range(0, threadCount)
            .Select(threadIdx => new Thread(() =>
            {
                var rng = new Random(threadIdx * 31);
                for (int iter = 0; iter < itersPerThread; iter++)
                {
                    try
                    {
                        string term = rng.Next(2) == 0
                            ? $"alpha{rng.Next(10)}"
                            : $"beta{rng.Next(5)}";

                        var hits = searcher.Search(new TermQuery("body", term), docCount);
                        int expected = referenceDocFreqs[term];

                        if (hits.TotalHits != expected)
                            errors.Add($"Thread {threadIdx}, iter {iter}: term={term}, expected {expected}, got {hits.TotalHits}");
                    }
                    catch (Exception ex)
                    {
                        exceptions.Add(ex);
                    }
                }
            }))
            .ToList();

        foreach (var t in threads) t.Start();
        foreach (var t in threads) t.Join(TimeSpan.FromSeconds(30));

        Assert.Empty(exceptions);
        Assert.Empty(errors);
    }

    [Fact]
    public void ConcurrentPostingsEnum_SameTermDifferentThreads_ReturnConsistentDocIds()
    {
        const int docCount = 120;
        const int threadCount = 12;

        var subDir = Path.Combine(_dir, "postings");
        Directory.CreateDirectory(subDir);

        var directory = new MMapDirectory(subDir);
        using var writer = new IndexWriter(directory, new IndexWriterConfig { MaxBufferedDocs = 300 });

        for (int i = 0; i < docCount; i++)
        {
            var doc = new LeanDocument();
            doc.Add(new TextField("body", i % 4 == 0 ? "target term present" : "other content here"));
            writer.AddDocument(doc);
        }
        writer.Commit();

        using var searcher = new IndexSearcher(directory);

        var reference = searcher.Search(new TermQuery("body", "target"), docCount);
        var expectedIds = reference.ScoreDocs.Select(sd => sd.DocId).OrderBy(id => id).ToArray();

        var errors = new System.Collections.Concurrent.ConcurrentBag<string>();

        var threads = Enumerable.Range(0, threadCount)
            .Select(threadIdx => new Thread(() =>
            {
                for (int iter = 0; iter < 50; iter++)
                {
                    var hits = searcher.Search(new TermQuery("body", "target"), docCount);
                    var actual = hits.ScoreDocs.Select(sd => sd.DocId).OrderBy(id => id).ToArray();
                    if (!actual.SequenceEqual(expectedIds))
                        errors.Add($"Thread {threadIdx}, iter {iter}: expected [{string.Join(",", expectedIds)}], got [{string.Join(",", actual)}]");
                }
            }))
            .ToList();

        foreach (var t in threads) t.Start();
        foreach (var t in threads) t.Join(TimeSpan.FromSeconds(30));

        Assert.Empty(errors);
    }
}
