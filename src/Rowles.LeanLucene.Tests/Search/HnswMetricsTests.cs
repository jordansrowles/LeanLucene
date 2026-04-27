using Rowles.LeanLucene.Codecs;
using Rowles.LeanLucene.Diagnostics;
using Rowles.LeanLucene.Document;
using Rowles.LeanLucene.Document.Fields;
using Rowles.LeanLucene.Index;
using Rowles.LeanLucene.Search;
using Rowles.LeanLucene.Search.Queries;
using Rowles.LeanLucene.Search.Searcher;
using Rowles.LeanLucene.Store;
using Rowles.LeanLucene.Tests.Fixtures;

namespace Rowles.LeanLucene.Tests.Search;

[Trait("Category", "Phase5")]
public sealed class HnswMetricsTests : IClassFixture<TestDirectoryFixture>
{
    private readonly TestDirectoryFixture _fixture;

    public HnswMetricsTests(TestDirectoryFixture fixture)
    {
        _fixture = fixture;
    }

    private string SubDir(string name)
    {
        var path = Path.Combine(_fixture.Path, name);
        Directory.CreateDirectory(path);
        return path;
    }

    private static (MMapDirectory Dir, DefaultMetricsCollector Metrics) BuildIndex(string name, int docCount)
    {
        var dir = new MMapDirectory(name);
        var metrics = new DefaultMetricsCollector();
        var cfg = new IndexWriterConfig
        {
            BuildHnswOnFlush = true,
            NormaliseVectors = true,
            HnswBuildConfig = new HnswBuildConfig { M = 8, M0 = 16, EfConstruction = 50 },
            HnswSeed = 42L,
            Metrics = metrics,
        };
        using (var writer = new IndexWriter(dir, cfg))
        {
            var rnd = new Random(123);
            for (int i = 0; i < docCount; i++)
            {
                var v = new float[16];
                for (int d = 0; d < v.Length; d++)
                    v[d] = (float)(rnd.NextDouble() * 2 - 1);
                var doc = new LeanDocument();
                doc.Add(new VectorField("embedding", new ReadOnlyMemory<float>(v)));
                writer.AddDocument(doc);
            }
            writer.Commit();
        }
        return (dir, metrics);
    }

    [Fact]
    public void HnswBuildOnFlush_RecordsHnswBuildMetric()
    {
        var (_, metrics) = BuildIndex(SubDir("hnsw_metrics_build"), docCount: 50);

        var snapshot = metrics.GetSnapshot();
        Assert.Equal(1, snapshot.HnswBuildCount);
        Assert.Equal(50, snapshot.HnswNodesBuilt);
    }

    [Fact]
    public void HnswSearch_RecordsHnswSearchMetric()
    {
        var dir = new MMapDirectory(SubDir("hnsw_metrics_search"));
        var writerMetrics = new DefaultMetricsCollector();
        var cfg = new IndexWriterConfig
        {
            BuildHnswOnFlush = true,
            NormaliseVectors = true,
            HnswSeed = 42L,
            Metrics = writerMetrics,
        };
        var rnd = new Random(7);
        using (var writer = new IndexWriter(dir, cfg))
        {
            for (int i = 0; i < 30; i++)
            {
                var v = new float[8];
                for (int d = 0; d < v.Length; d++) v[d] = (float)(rnd.NextDouble() * 2 - 1);
                var doc = new LeanDocument();
                doc.Add(new VectorField("embedding", new ReadOnlyMemory<float>(v)));
                writer.AddDocument(doc);
            }
            writer.Commit();
        }

        var searcherMetrics = new DefaultMetricsCollector();
        using var searcher = new IndexSearcher(dir, new IndexSearcherConfig { Metrics = searcherMetrics });

        var query = new float[8];
        for (int d = 0; d < query.Length; d++) query[d] = (float)(rnd.NextDouble() * 2 - 1);

        searcher.Search(new VectorQuery("embedding", query, topK: 5), 5);

        var snapshot = searcherMetrics.GetSnapshot();
        Assert.Equal(1, snapshot.HnswSearchCount);
        Assert.True(snapshot.HnswNodesVisited > 0, "Nodes visited should be positive.");
    }

    [Fact]
    public void Explain_VectorQuery_ReportsHnswStrategyAndScore()
    {
        var (dir, _) = BuildIndex(SubDir("hnsw_explain"), docCount: 40);
        using var searcher = new IndexSearcher(dir);

        var rnd = new Random(99);
        var query = new float[16];
        for (int d = 0; d < query.Length; d++) query[d] = (float)(rnd.NextDouble() * 2 - 1);

        var top = searcher.Search(new VectorQuery("embedding", query, topK: 3), 3);
        Assert.True(top.TotalHits > 0);

        var explanation = searcher.Explain(new VectorQuery("embedding", query, topK: 3), top.ScoreDocs[0].DocId);
        Assert.NotNull(explanation);
        Assert.Contains("HNSW two-phase", explanation!.Description);
        Assert.NotNull(explanation.Details);
        Assert.Contains(explanation.Details!, d => d.Description!.StartsWith("efSearch=", StringComparison.Ordinal));
        Assert.Contains(explanation.Details!, d => d.Description!.StartsWith("hnswNodeCount=", StringComparison.Ordinal));
    }
}
