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
        return [defaultCount];
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
}
