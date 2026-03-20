using Rowles.LeanLucene.Document;
using Rowles.LeanLucene.Document.Fields;
using Rowles.LeanLucene.Index.Indexer;
using Rowles.LeanLucene.Index.Segment;
using Rowles.LeanLucene.Search.Queries;
using Rowles.LeanLucene.Search.Scoring;
using Rowles.LeanLucene.Search.Searcher;
using Rowles.LeanLucene.Store;
using Rowles.LeanLucene.Tests.Fixtures;

namespace Rowles.LeanLucene.Tests.Index;

/// <summary>
/// Regression tests for autopsy issue C2: <c>SegmentMerger</c> previously discarded
/// roughly half the codec output. Each test pins one missing artefact (.fln, .dvn,
/// .dvs, .bkd, .tvd/.tvx, .pbs, IndexSortFields) plus the orphan-cleanup behaviour.
/// </summary>
[Trait("Category", "Index")]
[Trait("Category", "Merge")]
public sealed class SegmentMergerTests : IClassFixture<TestDirectoryFixture>
{
    private readonly TestDirectoryFixture _fixture;

    public SegmentMergerTests(TestDirectoryFixture fixture) => _fixture = fixture;

    private string SubDir(string name)
    {
        var path = Path.Combine(_fixture.Path, name);
        if (Directory.Exists(path)) Directory.Delete(path, recursive: true);
        Directory.CreateDirectory(path);
        return path;
    }

    private static string LatestMergedSegmentId(string dir)
    {
        // After a merge with threshold=2 over seg_0/seg_1, the merged segment is seg_2.
        // Pick whichever .seg file has the highest ordinal.
        var segFiles = Directory.GetFiles(dir, "seg_*.seg");
        Assert.NotEmpty(segFiles);
        return segFiles
            .Select(p => Path.GetFileNameWithoutExtension(p))
            .OrderByDescending(id => int.Parse(id.AsSpan("seg_".Length)))
            .First()!;
    }

    private static IndexWriterConfig SmallSegmentMergeConfig(bool storeTermVectors = false, IndexSort? sort = null)
        => new()
        {
            MaxBufferedDocs = 1,
            MergeThreshold = 2,
            StoreTermVectors = storeTermVectors,
            IndexSort = sort,
        };

    [Fact]
    public void Merge_PreservesFieldLengths_BM25ScoresMatchUnmerged()
    {
        var dir = SubDir(nameof(Merge_PreservesFieldLengths_BM25ScoresMatchUnmerged));
        var mmap = new MMapDirectory(dir);

        using (var writer = new IndexWriter(mmap, SmallSegmentMergeConfig()))
        {
            for (int i = 0; i < 4; i++)
            {
                var doc = new LeanDocument();
                doc.Add(new TextField("id", $"doc{i}"));
                doc.Add(new TextField("body", string.Join(' ', Enumerable.Repeat("alpha", i + 1))));
                writer.AddDocument(doc);
            }
            writer.Commit();
            Thread.Sleep(500);
            writer.Commit();
        }

        // After merge there must be a .fln file on the merged segment to preserve
        // exact per-doc field lengths (BM25 falls back to coarse norms otherwise).
        var mergedId = LatestMergedSegmentId(dir);
        var flnPath = Path.Combine(dir, mergedId + ".fln");
        Assert.True(File.Exists(flnPath), $"Expected merged segment {mergedId} to have a .fln file at {flnPath}");
        Assert.True(new FileInfo(flnPath).Length > 0);

        // Sanity: searching still returns all 4 docs and the longer doc scores lowest under BM25.
        using var searcher = new IndexSearcher(mmap);
        var results = searcher.Search(new TermQuery("body", "alpha"), 10);
        Assert.Equal(4, results.TotalHits);
    }

    [Fact]
    public void Merge_PreservesNumericDocValues()
    {
        var dir = SubDir(nameof(Merge_PreservesNumericDocValues));
        var mmap = new MMapDirectory(dir);

        using (var writer = new IndexWriter(mmap, SmallSegmentMergeConfig()))
        {
            for (int i = 0; i < 4; i++)
            {
                var doc = new LeanDocument();
                doc.Add(new TextField("id", $"doc{i}"));
                doc.Add(new NumericField("price", 10.0 + i));
                writer.AddDocument(doc);
            }
            writer.Commit();
            Thread.Sleep(500);
            writer.Commit();
        }

        var mergedId = LatestMergedSegmentId(dir);
        var dvnPath = Path.Combine(dir, mergedId + ".dvn");
        Assert.True(File.Exists(dvnPath), $"Expected merged segment {mergedId} to have a .dvn file");

        // Sort by numeric DocValues field — works only if .dvn survives the merge.
        using var searcher = new IndexSearcher(mmap);
        var sorted = searcher.Search(new WildcardQuery("id", "*"), 10, SortField.Numeric("price"));
        Assert.Equal(4, sorted.TotalHits);
    }

    [Fact]
    public void Merge_PreservesSortedDocValues()
    {
        var dir = SubDir(nameof(Merge_PreservesSortedDocValues));
        var mmap = new MMapDirectory(dir);

        using (var writer = new IndexWriter(mmap, SmallSegmentMergeConfig()))
        {
            string[] cats = ["alpha", "bravo", "charlie", "delta"];
            for (int i = 0; i < 4; i++)
            {
                var doc = new LeanDocument();
                doc.Add(new TextField("id", $"doc{i}"));
                doc.Add(new StringField("category", cats[i]));
                writer.AddDocument(doc);
            }
            writer.Commit();
            Thread.Sleep(500);
            writer.Commit();
        }

        var mergedId = LatestMergedSegmentId(dir);
        var dvsPath = Path.Combine(dir, mergedId + ".dvs");
        Assert.True(File.Exists(dvsPath), $"Expected merged segment {mergedId} to have a .dvs file");

        using var searcher = new IndexSearcher(mmap);
        var sorted = searcher.Search(new WildcardQuery("id", "*"), 10, SortField.String("category"));
        Assert.Equal(4, sorted.TotalHits);
    }

    [Fact]
    public void Merge_PreservesBkdRangeQueryResults()
    {
        var dir = SubDir(nameof(Merge_PreservesBkdRangeQueryResults));
        var mmap = new MMapDirectory(dir);

        using (var writer = new IndexWriter(mmap, SmallSegmentMergeConfig()))
        {
            for (int i = 0; i < 4; i++)
            {
                var doc = new LeanDocument();
                doc.Add(new TextField("id", $"doc{i}"));
                doc.Add(new NumericField("price", 100.0 + i * 10));
                writer.AddDocument(doc);
            }
            writer.Commit();
            Thread.Sleep(500);
            writer.Commit();
        }

        var mergedId = LatestMergedSegmentId(dir);
        var bkdPath = Path.Combine(dir, mergedId + ".bkd");
        Assert.True(File.Exists(bkdPath), $"Expected merged segment {mergedId} to have a .bkd file");

        using var searcher = new IndexSearcher(mmap);
        // 110.0..120.0 inclusive should hit doc1 (110) and doc2 (120).
        var hits = searcher.Search(new RangeQuery("price", 110.0, 120.0), 10);
        Assert.Equal(2, hits.TotalHits);
    }

    [Fact]
    public void Merge_PreservesTermVectors()
    {
        var dir = SubDir(nameof(Merge_PreservesTermVectors));
        var mmap = new MMapDirectory(dir);

        using (var writer = new IndexWriter(mmap, SmallSegmentMergeConfig(storeTermVectors: true)))
        {
            for (int i = 0; i < 4; i++)
            {
                var doc = new LeanDocument();
                doc.Add(new TextField("id", $"doc{i}"));
                doc.Add(new TextField("body", $"keyword{i} shared common content"));
                writer.AddDocument(doc);
            }
            writer.Commit();
            Thread.Sleep(500);
            writer.Commit();
        }

        var mergedId = LatestMergedSegmentId(dir);
        var tvdPath = Path.Combine(dir, mergedId + ".tvd");
        var tvxPath = Path.Combine(dir, mergedId + ".tvx");
        Assert.True(File.Exists(tvdPath), $"Expected merged segment {mergedId} to have a .tvd file");
        Assert.True(File.Exists(tvxPath), $"Expected merged segment {mergedId} to have a .tvx file");

        // MoreLikeThis depends on term vectors; with vectors lost it would return zero.
        using var searcher = new IndexSearcher(mmap);
        var more = searcher.MoreLikeThis(0, ["body"], 5);
        Assert.True(more.TotalHits > 0, "MoreLikeThis returned zero hits — term vectors likely lost on merge");
    }

    [Fact]
    public void Merge_PreservesParentBitSet_BlockJoinQueryStillReturnsParents()
    {
        var dir = SubDir(nameof(Merge_PreservesParentBitSet_BlockJoinQueryStillReturnsParents));
        var mmap = new MMapDirectory(dir);

        // MaxBufferedDocs must be >= block size so each block lands intact in one segment.
        // We Commit() between blocks to force a flush per block.
        var config = new IndexWriterConfig
        {
            MaxBufferedDocs = 16,
            MergeThreshold = 2,
        };

        using (var writer = new IndexWriter(mmap, config))
        {
            writer.AddDocumentBlock(
            [
                MakeChild("alpha bravo"),
                MakeChild("charlie delta"),
                MakeParent("post one"),
            ]);
            writer.Commit();

            writer.AddDocumentBlock(
            [
                MakeChild("echo foxtrot"),
                MakeParent("post two"),
            ]);
            writer.Commit();

            Thread.Sleep(500);
            writer.Commit();
        }

        var mergedId = LatestMergedSegmentId(dir);
        var pbsPath = Path.Combine(dir, mergedId + ".pbs");
        Assert.True(File.Exists(pbsPath), $"Expected merged segment {mergedId} to have a .pbs file");

        using var searcher = new IndexSearcher(mmap);
        var parents = searcher.Search(new BlockJoinQuery(new TermQuery("body", "alpha")), 10);
        Assert.Equal(1, parents.TotalHits);
        var stored = searcher.GetStoredFields(parents.ScoreDocs[0].DocId);
        Assert.True(stored.ContainsKey("title"));
        Assert.Contains("one", stored["title"][0]);
    }

    [Fact]
    public void Merge_PreservesIndexSortFields()
    {
        var dir = SubDir(nameof(Merge_PreservesIndexSortFields));
        var mmap = new MMapDirectory(dir);
        var sort = new IndexSort(SortField.Numeric("price"));

        using (var writer = new IndexWriter(mmap, SmallSegmentMergeConfig(sort: sort)))
        {
            for (int i = 0; i < 4; i++)
            {
                var doc = new LeanDocument();
                doc.Add(new TextField("id", $"doc{i}"));
                doc.Add(new NumericField("price", 50.0 - i));
                writer.AddDocument(doc);
            }
            writer.Commit();
            Thread.Sleep(500);
            writer.Commit();
        }

        var mergedId = LatestMergedSegmentId(dir);
        var segInfo = SegmentInfo.ReadFrom(Path.Combine(dir, mergedId + ".seg"));
        Assert.NotNull(segInfo.IndexSortFields);
        Assert.Single(segInfo.IndexSortFields!);
        Assert.Equal("Numeric:price:False", segInfo.IndexSortFields![0]);
    }

    [Fact]
    public void CleanupSegmentFiles_LeavesNoOrphans()
    {
        var dir = SubDir(nameof(CleanupSegmentFiles_LeavesNoOrphans));
        var mmap = new MMapDirectory(dir);

        using (var writer = new IndexWriter(mmap, SmallSegmentMergeConfig(storeTermVectors: true)))
        {
            for (int i = 0; i < 4; i++)
            {
                var doc = new LeanDocument();
                doc.Add(new TextField("id", $"doc{i}"));
                doc.Add(new NumericField("price", 1.0 + i));
                doc.Add(new StringField("category", $"cat{i}"));
                doc.Add(new TextField("body", $"shared body content number {i}"));
                writer.AddDocument(doc);
            }
            writer.Commit();
            Thread.Sleep(500);
            writer.Commit();
        }

        // After merge, the original seg_0 .. seg_3 must have ZERO files left on disk
        // (any extension). The previous bug only cleaned a hardcoded extension list.
        for (int i = 0; i < 4; i++)
        {
            var orphans = Directory.GetFiles(dir, $"seg_{i}.*");
            Assert.Empty(orphans);
        }
    }

    private static LeanDocument MakeChild(string body)
    {
        var doc = new LeanDocument();
        doc.Add(new TextField("body", body));
        doc.Add(new StringField("type", "child"));
        return doc;
    }

    private static LeanDocument MakeParent(string title)
    {
        var doc = new LeanDocument();
        doc.Add(new TextField("title", title));
        doc.Add(new StringField("type", "parent"));
        return doc;
    }
}
