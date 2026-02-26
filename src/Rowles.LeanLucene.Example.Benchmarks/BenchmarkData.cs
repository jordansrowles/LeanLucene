using System.Globalization;

namespace Rowles.LeanLucene.Example.Benchmarks;

internal static class BenchmarkData
{
    private static readonly string[] Topics =
    [
        "catalog",
        "ranking",
        "analytics",
        "ingestion",
        "compression",
        "vectorization",
        "querying",
        "throughput"
    ];

    private static readonly string[] Domains =
    [
        "ecommerce",
        "observability",
        "knowledgebase",
        "documentation",
        "telemetry",
        "support",
        "security",
        "monitoring"
    ];

    /// <summary>Default document count used by all benchmark suites when <c>BENCH_DOC_COUNT</c> is not set.</summary>
    public const int DefaultDocCount = 1_000;

    /// <summary>
    /// Returns the document count to use for benchmarks. When the BENCH_DOC_COUNT
    /// environment variable is set (e.g. via --doccount in benchmark.ps1), that
    /// value is used; otherwise the per-suite default is returned.
    /// </summary>
    public static IEnumerable<int> GetDocCounts(int defaultCount)
    {
        var env = Environment.GetEnvironmentVariable("BENCH_DOC_COUNT");
        if (int.TryParse(env, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) && n > 0)
            return [n];
        return [100, 1_000, 10_000, 100_000, 500_000, 1_000_000, 3_000_000];
    }

    public static string[] BuildDocuments(int count)
    {
        var docs = new string[count];
        for (int i = 0; i < count; i++)
        {
            var keyword = (i % 3) switch
            {
                0 => "search",
                1 => "vector",
                _ => "performance"
            };

            var topic = Topics[i % Topics.Length];
            var domain = Domains[(i * 7) % Domains.Length];

            docs[i] =
                $"doc {i} {keyword} benchmark for {domain} {topic} " +
                "dotnet segment index bm25 retrieval latency throughput memory mapped files";
        }

        return docs;
    }

    /// <summary>Builds documents with a numeric "price" field for index sort benchmarks.</summary>
    public static (string Body, double Price)[] BuildDocumentsWithPrices(int count)
    {
        var rng = new Random(42);
        var docs = new (string Body, double Price)[count];
        for (int i = 0; i < count; i++)
        {
            var topic = Topics[i % Topics.Length];
            var domain = Domains[(i * 7) % Domains.Length];
            docs[i] = (
                $"product {i} {topic} {domain} benchmark item search retrieval",
                Math.Round(rng.NextDouble() * 999 + 1, 2)
            );
        }
        return docs;
    }

    /// <summary>Builds parent-child document blocks for block join benchmarks.</summary>
    public static (string ParentTitle, string[] ChildBodies)[] BuildParentChildBlocks(int blockCount, int childrenPerBlock = 3)
    {
        var blocks = new (string ParentTitle, string[] ChildBodies)[blockCount];
        for (int i = 0; i < blockCount; i++)
        {
            var topic = Topics[i % Topics.Length];
            var children = new string[childrenPerBlock];
            for (int c = 0; c < childrenPerBlock; c++)
                children[c] = $"child {c} comment on {topic} discussion reply search content";
            blocks[i] = ($"parent {i} {topic} post article", children);
        }
        return blocks;
    }

    /// <summary>Builds misspelled term variants for suggester benchmarks.</summary>
    public static (string Original, string Misspelled)[] BuildMisspelledTerms()
    {
        return
        [
            ("search", "serch"),
            ("vector", "vecor"),
            ("performance", "performnce"),
            ("benchmark", "benchmrk"),
            ("throughput", "througput"),
            ("analytics", "anlytics"),
            ("compression", "compresion"),
            ("retrieval", "retreival"),
            ("ingestion", "ingesion"),
            ("querying", "queryin"),
            ("catalog", "catlog"),
            ("monitoring", "monitring"),
            ("security", "secrity"),
            ("telemetry", "telmetry"),
            ("documentation", "documention"),
            ("latency", "lateny"),
            ("segment", "segmnt"),
            ("memory", "memry"),
            ("indexing", "indexng"),
            ("mapping", "mappng"),
        ];
    }

    /// <summary>Builds JSON document strings for JSON mapping benchmarks.</summary>
    public static string[] BuildJsonDocuments(int count)
    {
        var docs = new string[count];
        for (int i = 0; i < count; i++)
        {
            var topic = Topics[i % Topics.Length];
            var domain = Domains[(i * 7) % Domains.Length];
            docs[i] = $$"""
                {"id":{{i}},"title":"{{topic}} article {{i}}","body":"{{domain}} {{topic}} search benchmark content","price":{{(i * 9.99 + 1):F2}},"active":true,"tags":["{{topic}}","{{domain}}"]}
                """;
        }
        return docs;
    }
}
