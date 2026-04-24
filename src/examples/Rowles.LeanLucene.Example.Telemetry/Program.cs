using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Rowles.LeanLucene.Diagnostics;
using Rowles.LeanLucene.Document;
using Rowles.LeanLucene.Document.Fields;
using Rowles.LeanLucene.Index.Indexer;
using Rowles.LeanLucene.Search.Queries;
using Rowles.LeanLucene.Search.Searcher;
using Rowles.LeanLucene.Store;

var builder = Host.CreateApplicationBuilder(args);

// ── OpenTelemetry ────────────────────────────────────────────────────────────

const string OtlpEndpoint = "http://localhost:4317";

builder.Services
    .AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("LeanLucene.Telemetry"))
    .WithTracing(tracing => tracing
        .AddSource("Rowles.LeanLucene")
        .AddOtlpExporter(o => o.Endpoint = new Uri(OtlpEndpoint)))
    .WithMetrics(metrics => metrics
        .AddMeter("Rowles.LeanLucene")
        .AddRuntimeInstrumentation()
        .AddOtlpExporter(o => o.Endpoint = new Uri(OtlpEndpoint)));

// ── Demo worker ──────────────────────────────────────────────────────────────

builder.Services.AddHostedService<DemoWorker>();

var host = builder.Build();
await host.RunAsync();

// ── DemoWorker ───────────────────────────────────────────────────────────────

/// <summary>
/// Runs a continuous index/search loop so traces and metrics are visible in the Aspire dashboard.
/// </summary>
internal sealed class DemoWorker : BackgroundService
{
    private const string OtlpEndpoint = "http://localhost:4317";

    private static readonly string[] Titles =
    [
        "The Rust Programming Language",
        "Clean Code: A Handbook of Agile Software Craftsmanship",
        "Domain-Driven Design",
        "Designing Data-Intensive Applications",
        "The Pragmatic Programmer",
        "Structure and Interpretation of Computer Programs",
        "Introduction to Algorithms",
        "Code Complete",
        "Refactoring: Improving the Design of Existing Code",
        "Working Effectively with Legacy Code",
    ];

    private static readonly string[] Queries =
    [
        "programming", "design", "algorithms", "code", "data", "refactoring",
        "agile", "software", "computer", "structures",
    ];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var indexPath = Path.Combine(Path.GetTempPath(), "leanlucene-telemetry-demo");
        Directory.CreateDirectory(indexPath);

        var metrics    = new MeterMetricsCollector();
        var dir        = new MMapDirectory(indexPath);

        var writerConfig = new IndexWriterConfig { Metrics = metrics };
        using var writer = new IndexWriter(dir, writerConfig);

        var searcherConfig = new IndexSearcherConfig
        {
            Metrics         = metrics,
            EnableQueryCache = true,
        };

        // Seed the index with sample documents
        Console.WriteLine("Seeding index...");
        for (int i = 0; i < Titles.Length; i++)
        {
            var doc = new LeanDocument();
            doc.Add(new TextField("title", Titles[i]));
            doc.Add(new StringField("id", i.ToString()));
            writer.AddDocument(doc);
        }
        writer.Commit();
        Console.WriteLine($"Seeded {Titles.Length} documents. Sending telemetry to {OtlpEndpoint}...");

        using var searcher = new IndexSearcher(dir, searcherConfig);

        int iteration = 0;
        while (!stoppingToken.IsCancellationRequested)
        {
            string q = Queries[iteration % Queries.Length];
            var results = searcher.Search(new TermQuery("title", q), topN: 5);
            Console.WriteLine($"[{iteration,4}] '{q}' → {results.TotalHits} hit(s)");

            // Trigger a cache hit on every other iteration
            if (iteration % 2 == 0)
                searcher.Search(new TermQuery("title", q), topN: 5);

            iteration++;

            // Periodically add a new document and commit to trigger flush + merge traces
            if (iteration % 10 == 0)
            {
                var doc = new LeanDocument();
                doc.Add(new TextField("title", $"Dynamic Document {iteration}"));
                doc.Add(new StringField("id", $"dyn-{iteration}"));
                writer.AddDocument(doc);
                writer.Commit();
                Console.WriteLine($"  ↳ Committed doc dyn-{iteration}");
            }

            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }

        Console.WriteLine("Demo finished.");
        metrics.Dispose();
    }
}
