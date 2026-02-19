using System.Text.Json;
using Rowles.LeanLucene.DevBench;
using Rowles.LeanLucene.Document;
using Rowles.LeanLucene.Document.Fields;
using Rowles.LeanLucene.Index.Indexer;
using Rowles.LeanLucene.Search;
using Rowles.LeanLucene.Search.Queries;
using Rowles.LeanLucene.Search.Scoring;
using Rowles.LeanLucene.Search.Searcher;
using Rowles.LeanLucene.Store;

Console.WriteLine("╔══════════════════════════════════════╗");
Console.WriteLine("║     LeanLucene DevBench v0.1         ║");
Console.WriteLine($"║  Runtime: {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,-27}║");
Console.WriteLine("╚══════════════════════════════════════╝");

string suite = args.Length > 0 ? args[0] : "baseline";

switch (suite.ToLowerInvariant())
{
    case "baseline":
        RunBaseline();
        break;
    case "self":
        RunSelfBench();
        break;
    default:
        Console.WriteLine($"Unknown suite: {suite}. Available: baseline, self");
        break;
}

void RunSelfBench()
{
    // Validate that MicroBench overhead is < 1μs
    int counter = 0;
    var result = MicroBench.Measure("Noop (overhead check)", 100, 10_000, () => { counter++; });
    MicroBench.PrintHeader("Self-Test");
    MicroBench.Print(result);

    if (result.MeanUs > 1.0)
        Console.WriteLine($"  ⚠ WARNING: Measurement overhead is {result.MeanUs:F2} μs — expected < 1 μs");
    else
        Console.WriteLine($"  ✓ Measurement overhead: {result.MeanUs:F2} μs (OK)");
}

void RunBaseline()
{
    const int docCount = 10_000;
    string indexPath = Path.Combine(Path.GetTempPath(), "leanlucene-devbench-" + Guid.NewGuid().ToString("N")[..8]);

    try
    {
        // Pre-generate documents
        var docs = GenerateDocuments(docCount);

        // === Indexing benchmarks ===
        MicroBench.PrintHeader("Indexing");

        var indexResult = MicroBench.Measure("Index 10K docs", 0, 1, () =>
        {
            if (Directory.Exists(indexPath)) Directory.Delete(indexPath, true);
            var dir = new MMapDirectory(indexPath);
            var config = new IndexWriterConfig { MaxBufferedDocs = 5_000, RamBufferSizeMB = 64 };
            using var writer = new IndexWriter(dir, config);
            foreach (var doc in docs)
                writer.AddDocument(doc);
            writer.Commit();
        });
        MicroBench.Print(indexResult);

        // === Search benchmarks ===
        MicroBench.PrintHeader("Search (single iteration)");
        var results = new List<MicroBench.Result>();

        var searchDir = new MMapDirectory(indexPath);
        using var searcher = new IndexSearcher(searchDir);

        results.Add(MicroBench.Measure("TermQuery", 3, 100, () =>
            searcher.Search(new TermQuery("body", "search"), 10)));

        results.Add(MicroBench.Measure("BooleanQuery (MUST)", 3, 100, () =>
        {
            var bq = new BooleanQuery();
            bq.Add(new TermQuery("body", "search"), Occur.Must);
            bq.Add(new TermQuery("body", "engine"), Occur.Must);
            searcher.Search(bq, 10);
        }));

        results.Add(MicroBench.Measure("BooleanQuery (SHOULD)", 3, 100, () =>
        {
            var bq = new BooleanQuery();
            bq.Add(new TermQuery("body", "search"), Occur.Should);
            bq.Add(new TermQuery("body", "performance"), Occur.Should);
            searcher.Search(bq, 10);
        }));

        results.Add(MicroBench.Measure("BooleanQuery (MUST_NOT)", 3, 100, () =>
        {
            var bq = new BooleanQuery();
            bq.Add(new TermQuery("body", "search"), Occur.Must);
            bq.Add(new TermQuery("body", "slow"), Occur.MustNot);
            searcher.Search(bq, 10);
        }));

        results.Add(MicroBench.Measure("PhraseQuery (2 terms)", 3, 100, () =>
            searcher.Search(new PhraseQuery("body", "search", "engine"), 10)));

        results.Add(MicroBench.Measure("PrefixQuery", 3, 100, () =>
            searcher.Search(new PrefixQuery("body", "sear"), 10)));

        results.Add(MicroBench.Measure("WildcardQuery", 3, 100, () =>
            searcher.Search(new WildcardQuery("body", "sear*"), 10)));

        results.Add(MicroBench.Measure("FuzzyQuery", 3, 100, () =>
            searcher.Search(new FuzzyQuery("body", "serch", 1), 10)));

        foreach (var r in results)
            MicroBench.Print(r);

        // === Save results ===
        var allResults = new List<MicroBench.Result> { indexResult };
        allResults.AddRange(results);
        SaveResults(allResults);
    }
    finally
    {
        if (Directory.Exists(indexPath))
            Directory.Delete(indexPath, true);
    }
}

List<LeanDocument> GenerateDocuments(int count)
{
    var random = new Random(42);
    var words = new[] { "search", "engine", "performance", "index", "query", "document",
        "field", "token", "filter", "analyser", "segment", "merge", "commit", "flush",
        "buffer", "memory", "allocation", "bitmap", "postings", "term", "dictionary",
        "fuzzy", "prefix", "wildcard", "boolean", "phrase", "score", "ranking", "fast",
        "slow", "optimise", "compress", "encode", "decode", "block", "skip", "advance" };

    var docs = new List<LeanDocument>(count);
    for (int i = 0; i < count; i++)
    {
        var doc = new LeanDocument();
        doc.Add(new StringField("id", $"doc-{i}"));
        doc.Add(new TextField("title", GenerateSentence(random, words, 5, 10)));
        doc.Add(new TextField("body", GenerateSentence(random, words, 20, 60)));
        doc.Add(new StringField("category", words[random.Next(words.Length)]));
        docs.Add(doc);
    }
    return docs;
}

string GenerateSentence(Random rng, string[] words, int minWords, int maxWords)
{
    int len = rng.Next(minWords, maxWords + 1);
    var sb = new System.Text.StringBuilder(len * 8);
    for (int i = 0; i < len; i++)
    {
        if (i > 0) sb.Append(' ');
        sb.Append(words[rng.Next(words.Length)]);
    }
    return sb.ToString();
}

void SaveResults(List<MicroBench.Result> results)
{
    var output = new
    {
        timestamp = DateTime.UtcNow.ToString("o"),
        runtime = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
        results = results.Select(r => new
        {
            name = r.Name,
            iterations = r.Iterations,
            meanUs = r.MeanUs,
            p50Us = r.P50Us,
            p99Us = r.P99Us,
            allocatedBytes = r.AllocatedBytes,
            gen0 = r.Gen0,
            gen1 = r.Gen1,
            gen2 = r.Gen2
        })
    };

    string json = JsonSerializer.Serialize(output, new JsonSerializerOptions { WriteIndented = true });
    Console.WriteLine();
    Console.WriteLine("═══ JSON Output ═══");
    Console.WriteLine(json);
}
