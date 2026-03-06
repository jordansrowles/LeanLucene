namespace Rowles.LeanLucene.Benchmarks.Quick;

/// <summary>
/// Builds synthetic test documents for the quick sanity benchmarks.
/// Deliberately simple and deterministic.
/// </summary>
internal static class SanityBenchmarkData
{
    private static readonly string[] Topics =
    [
        "catalog", "ranking", "analytics", "ingestion",
        "compression", "vectorization", "querying", "throughput"
    ];

    private static readonly string[] Domains =
    [
        "ecommerce", "observability", "knowledgebase", "documentation",
        "telemetry", "support", "security", "monitoring"
    ];

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

    /// <summary>
    /// Returns a small set of misspelled term pairs for suggester benchmarks.
    /// </summary>
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
        ];
    }
}
