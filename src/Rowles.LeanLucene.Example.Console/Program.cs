using Rowles.LeanLucene.Document;
using Rowles.LeanLucene.Document.Fields;
using Rowles.LeanLucene.Index;
using Rowles.LeanLucene.Index.Indexer;
using Rowles.LeanLucene.Search;
using Rowles.LeanLucene.Search.Queries;
using Rowles.LeanLucene.Search.Scoring;
using Rowles.LeanLucene.Search.Searcher;
using Rowles.LeanLucene.Store;

var indexPath = Path.Combine(
    AppContext.BaseDirectory,
    "data",
    "leanlucene-console-example");

if (Directory.Exists(indexPath))
    Directory.Delete(indexPath, recursive: true);

Directory.CreateDirectory(indexPath);

var sampleArticles = BuildSampleArticles();
var directory = new MMapDirectory(indexPath);
var writerConfig = new IndexWriterConfig
{
    MaxBufferedDocs = 256,
    RamBufferSizeMB = 16
};

using (var writer = new IndexWriter(directory, writerConfig))
{
    foreach (var article in sampleArticles)
        writer.AddDocument(ToDocument(article));

    writer.Commit();

    // Demonstrate delete semantics before opening the reader snapshot.
    writer.DeleteDocuments(new TermQuery("category", "archived"));
    writer.Commit();
}

using var searcher = new IndexSearcher(directory);

WriteSection("Index statistics");
Console.WriteLine($"Total docs: {searcher.Stats.TotalDocCount}");
Console.WriteLine($"Live docs : {searcher.Stats.LiveDocCount}");
Console.WriteLine($"Avg body length: {searcher.Stats.GetAvgFieldLength("body"):0.00}");

WriteSection("TermQuery: body:search");
PrintResults(
    searcher.Search(new TermQuery("body", "search"), topN: 10),
    sampleArticles);

WriteSection("PhraseQuery: \"fast search\"");
PrintResults(
    searcher.Search(new PhraseQuery("body", "fast", "search"), topN: 10),
    sampleArticles);

WriteSection("BooleanQuery: MUST body:search, SHOULD body:dotnet, MUST_NOT category:archived");
var booleanQuery = new BooleanQuery();
booleanQuery.Add(new TermQuery("body", "search"), Occur.Must);
booleanQuery.Add(new TermQuery("body", "dotnet"), Occur.Should);
booleanQuery.Add(new TermQuery("category", "archived"), Occur.MustNot);
PrintResults(searcher.Search(booleanQuery, topN: 10), sampleArticles);

WriteSection("RangeQuery: price [30..80]");
PrintResults(
    searcher.Search(new RangeQuery("price", 30, 80), topN: 10),
    sampleArticles);

WriteSection("VectorQuery: nearest embeddings");
var queryVector = new VectorQuery(
    field: "embedding",
    queryVector: [0.94f, 0.04f, 0.01f, 0.01f],
    topK: 3);
PrintResults(searcher.Search(queryVector, queryVector.TopK), sampleArticles);

WriteSection("Done");
Console.WriteLine($"Index files are in: {indexPath}");

return;

static LeanDocument ToDocument(SampleArticle article)
{
    var document = new LeanDocument();
    document.Add(new StringField("id", article.Id));
    document.Add(new StringField("category", article.Category));
    document.Add(new TextField("title", article.Title));
    document.Add(new TextField("body", article.Body));
    document.Add(new NumericField("price", article.Price));
    document.Add(new VectorField("embedding", article.Embedding));
    return document;
}

static void PrintResults(TopDocs results, IReadOnlyList<SampleArticle> articles)
{
    Console.WriteLine($"TotalHits: {results.TotalHits}");
    foreach (var hit in results.ScoreDocs)
    {
        string articleLabel = hit.DocId >= 0 && hit.DocId < articles.Count
            ? $"{articles[hit.DocId].Id} | {articles[hit.DocId].Title}"
            : $"doc-{hit.DocId}";

        Console.WriteLine($"  docId={hit.DocId,-2} score={hit.Score,8:0.0000}  {articleLabel}");
    }
}

static void WriteSection(string title)
{
    Console.WriteLine();
    Console.WriteLine(new string('=', 80));
    Console.WriteLine(title);
    Console.WriteLine(new string('=', 80));
}

static List<SampleArticle> BuildSampleArticles()
{
    return
    [
        new(
            "doc-001",
            "LeanLucene quickstart",
            "fast search indexing for dotnet services and APIs",
            "tutorial",
            49,
            new ReadOnlyMemory<float>([0.97f, 0.02f, 0.00f, 0.01f])),
        new(
            "doc-002",
            "Vector relevance ranking",
            "semantic search uses vector similarity for ranking",
            "search",
            79,
            new ReadOnlyMemory<float>([0.92f, 0.06f, 0.01f, 0.01f])),
        new(
            "doc-003",
            "Operating system internals",
            "memory mapped files and cache locality tips",
            "systems",
            59,
            new ReadOnlyMemory<float>([0.10f, 0.78f, 0.10f, 0.02f])),
        new(
            "doc-004",
            "Archived migration notes",
            "legacy search migration plan and retired infrastructure",
            "archived",
            35,
            new ReadOnlyMemory<float>([0.25f, 0.25f, 0.25f, 0.25f])),
        new(
            "doc-005",
            "Practical benchmark guide",
            "compare lucene and leanlucene for fast search workloads",
            "benchmark",
            99,
            new ReadOnlyMemory<float>([0.90f, 0.04f, 0.04f, 0.02f])),
        new(
            "doc-006",
            "Budget indexing handbook",
            "fast search on a budget with practical dotnet tuning",
            "tutorial",
            39,
            new ReadOnlyMemory<float>([0.89f, 0.05f, 0.03f, 0.03f]))
    ];
}

internal sealed record SampleArticle(
    string Id,
    string Title,
    string Body,
    string Category,
    double Price,
    ReadOnlyMemory<float> Embedding);
