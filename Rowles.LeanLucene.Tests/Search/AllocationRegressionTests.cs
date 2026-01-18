using System.Diagnostics;
using Rowles.LeanLucene.Analysis;
using Rowles.LeanLucene.Document;
using Rowles.LeanLucene.Index;
using Rowles.LeanLucene.Search;
using Rowles.LeanLucene.Store;
using Rowles.LeanLucene.Tests.Fixtures;
using Xunit.Abstractions;

namespace Rowles.LeanLucene.Tests.Search;

/// <summary>
/// Allocation regression guards for zero-alloc hot paths.
/// These tests verify that heavily-optimised paths (streaming BooleanQuery,
/// ArrayPool phrase positions, intern cache) maintain low allocation budgets.
/// </summary>
[Trait("Category", "Perf")]
[Trait("Category", "AllocationRegression")]
public sealed class AllocationRegressionTests : IClassFixture<TestDirectoryFixture>
{
    private readonly TestDirectoryFixture _fixture;
    private readonly ITestOutputHelper _output;

    public AllocationRegressionTests(TestDirectoryFixture fixture, ITestOutputHelper output)
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

    [Fact]
    public void BooleanQuery_Must_StreamingPath_LowAllocationPerQuery()
    {
        // Arrange: build a 500-doc index where ~half contain both target terms
        const int docCount = 500;
        const int warmup = 200;
        const int measured = 100;
        var dir = new MMapDirectory(SubDir("alloc_bool_must"));
        var rng = new Random(42);
        string[] pool = ["alpha", "beta", "gamma", "delta", "epsilon"];

        using (var writer = new IndexWriter(dir, new IndexWriterConfig { MaxBufferedDocs = 256 }))
        {
            for (int i = 0; i < docCount; i++)
            {
                var doc = new LeanDocument();
                var words = Enumerable.Range(0, 8).Select(_ => pool[rng.Next(pool.Length)]);
                doc.Add(new TextField("body", string.Join(" ", words)));
                writer.AddDocument(doc);
            }
            writer.Commit();
        }

        using var searcher = new IndexSearcher(dir);
        var query = new BooleanQuery();
        query.Add(new TermQuery("body", "alpha"), Occur.Must);
        query.Add(new TermQuery("body", "beta"), Occur.Must);

        // Warmup
        for (int i = 0; i < warmup; i++)
            searcher.Search(query, 25);

        // Measure
        long allocBefore = GC.GetAllocatedBytesForCurrentThread();
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < measured; i++)
            searcher.Search(query, 25);
        sw.Stop();
        long allocAfter = GC.GetAllocatedBytesForCurrentThread();

        double avgBytes = (double)(allocAfter - allocBefore) / measured;
        double avgUs = sw.Elapsed.TotalMicroseconds / measured;

        _output.WriteLine($"BooleanQuery Must(2 terms) over {docCount} docs:");
        _output.WriteLine($"  Avg allocation: {avgBytes:F0} bytes/query");
        _output.WriteLine($"  Avg latency:    {avgUs:F1} µs/query");
        _output.WriteLine($"  Total hits:     {searcher.Search(query, 25).TotalHits}");

        // Budget: ≤ 8 KB per query — regression guard (measured ~3.6KB, catches doublings)
        Assert.True(avgBytes <= 8192,
            $"BooleanQuery Must streaming path allocated {avgBytes:F0} bytes/query, budget is 8192 bytes");
    }

    [Fact]
    public void BooleanQuery_ShouldOnly_StreamingPath_LowAllocationPerQuery()
    {
        // Arrange
        const int docCount = 500;
        const int warmup = 200;
        const int measured = 100;
        var dir = new MMapDirectory(SubDir("alloc_bool_should"));
        var rng = new Random(42);
        string[] pool = ["red", "green", "blue", "yellow", "orange"];

        using (var writer = new IndexWriter(dir, new IndexWriterConfig { MaxBufferedDocs = 256 }))
        {
            for (int i = 0; i < docCount; i++)
            {
                var doc = new LeanDocument();
                var words = Enumerable.Range(0, 8).Select(_ => pool[rng.Next(pool.Length)]);
                doc.Add(new TextField("body", string.Join(" ", words)));
                writer.AddDocument(doc);
            }
            writer.Commit();
        }

        using var searcher = new IndexSearcher(dir);
        var query = new BooleanQuery();
        query.Add(new TermQuery("body", "red"), Occur.Should);
        query.Add(new TermQuery("body", "green"), Occur.Should);
        query.Add(new TermQuery("body", "blue"), Occur.Should);

        // Warmup
        for (int i = 0; i < warmup; i++)
            searcher.Search(query, 25);

        // Measure
        long allocBefore = GC.GetAllocatedBytesForCurrentThread();
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < measured; i++)
            searcher.Search(query, 25);
        sw.Stop();
        long allocAfter = GC.GetAllocatedBytesForCurrentThread();

        double avgBytes = (double)(allocAfter - allocBefore) / measured;
        double avgUs = sw.Elapsed.TotalMicroseconds / measured;

        _output.WriteLine($"BooleanQuery Should(3 terms) over {docCount} docs:");
        _output.WriteLine($"  Avg allocation: {avgBytes:F0} bytes/query");
        _output.WriteLine($"  Avg latency:    {avgUs:F1} µs/query");
        _output.WriteLine($"  Total hits:     {searcher.Search(query, 25).TotalHits}");

        // Budget: ≤ 8 KB per query — regression guard (measured ~4.6KB, catches doublings)
        Assert.True(avgBytes <= 8192,
            $"BooleanQuery Should streaming path allocated {avgBytes:F0} bytes/query, budget is 8192 bytes");
    }

    [Fact]
    public void PhraseQuery_ArrayPoolPositions_LowAllocationPerQuery()
    {
        // Arrange
        const int docCount = 500;
        const int warmup = 200;
        const int measured = 100;
        var dir = new MMapDirectory(SubDir("alloc_phrase"));
        var rng = new Random(42);
        string[] pool = ["quick", "brown", "fox", "jumps", "over", "lazy", "dog"];

        using (var writer = new IndexWriter(dir, new IndexWriterConfig { MaxBufferedDocs = 256 }))
        {
            for (int i = 0; i < docCount; i++)
            {
                var doc = new LeanDocument();
                var words = Enumerable.Range(0, 10).Select(_ => pool[rng.Next(pool.Length)]);
                doc.Add(new TextField("body", string.Join(" ", words)));
                writer.AddDocument(doc);
            }
            writer.Commit();
        }

        using var searcher = new IndexSearcher(dir);
        var query = new PhraseQuery("body", "quick", "brown");

        // Warmup
        for (int i = 0; i < warmup; i++)
            searcher.Search(query, 25);

        // Measure
        long allocBefore = GC.GetAllocatedBytesForCurrentThread();
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < measured; i++)
            searcher.Search(query, 25);
        sw.Stop();
        long allocAfter = GC.GetAllocatedBytesForCurrentThread();

        double avgBytes = (double)(allocAfter - allocBefore) / measured;
        double avgUs = sw.Elapsed.TotalMicroseconds / measured;

        _output.WriteLine($"PhraseQuery(\"quick\",\"brown\") over {docCount} docs:");
        _output.WriteLine($"  Avg allocation: {avgBytes:F0} bytes/query");
        _output.WriteLine($"  Avg latency:    {avgUs:F1} µs/query");
        _output.WriteLine($"  Total hits:     {searcher.Search(query, 25).TotalHits}");

        // Budget: ≤ 8 KB per query — regression guard (measured ~3.9KB, catches doublings)
        Assert.True(avgBytes <= 8192,
            $"PhraseQuery allocated {avgBytes:F0} bytes/query, budget is 8192 bytes");
    }

    [Fact]
    public void StandardAnalyser_InternCache_StableCorpusNoNewStringAllocations()
    {
        // After warmup on a stable corpus, repeated analysis should hit intern cache 100%
        const int warmup = 50;
        const int measured = 100;
        const string input = "the quick brown fox jumps over the lazy dog with some extra padding words here";

        var analyser = new StandardAnalyser();

        // Warmup — populate intern cache
        for (int i = 0; i < warmup; i++)
            analyser.Analyse(input);

        // Measure
        long allocBefore = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < measured; i++)
            analyser.Analyse(input);
        long allocAfter = GC.GetAllocatedBytesForCurrentThread();

        double avgBytes = (double)(allocAfter - allocBefore) / measured;

        _output.WriteLine($"StandardAnalyser intern cache (same {input.Split(' ').Length}-word input × {measured}):");
        _output.WriteLine($"  Avg allocation: {avgBytes:F0} bytes/call");

        // After warmup, only the offset buffer reuse and token list operations should allocate.
        // No new string allocations expected (all cached).
        Assert.True(avgBytes <= 256,
            $"StandardAnalyser allocated {avgBytes:F0} bytes/call after warmup, expected ≤ 256 (intern cache should be stable)");
    }
}
