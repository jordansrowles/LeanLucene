namespace Rowles.LeanLucene.Benchmarks.RealData;

/// <summary>
/// Measures search throughput on real Project Gutenberg ebook text.
/// Two indexes are built once in setup: one with <see cref="StandardAnalyser"/>
/// and one with <see cref="EnglishAnalyser"/>. The benchmark measures only the
/// hot search path.
/// </summary>
/// <remarks>
/// Query terms are pre-processed through each analyser's pipeline so that the
/// query matches what is stored in the index (the English analyser stems index
/// tokens, so the query term must also be stemmed for a correct lookup).
/// </remarks>
[MemoryDiagnoser]
[HtmlExporter]
[JsonExporterAttribute.Full]
[MarkdownExporterAttribute.GitHub]
[SimpleJob]
public class GutenbergSearchBenchmarks
{
    private const int TopN = 25;

    // Terms that appear across multiple books; root forms are preserved by Porter stemmer,
    // so results are directly comparable between analysers.
    [Params("love", "man", "night", "sea", "death")]
    public string SearchTerm { get; set; } = "love";

    // Pre-analysed query terms (may differ between analysers for morphologically rich terms)
    private string _standardQueryTerm = string.Empty;
    private string _englishQueryTerm  = string.Empty;

    private string _standardIndexPath = string.Empty;
    private string _englishIndexPath  = string.Empty;

    private MMapDirectory? _standardDirectory;
    private MMapDirectory? _englishDirectory;
    private IndexSearcher? _standardSearcher;
    private IndexSearcher? _englishSearcher;

    [GlobalSetup]
    public void Setup()
    {
        var paragraphs = GutenbergDataLoader.Load();

        _standardQueryTerm = AnalyseQueryTerm(new StandardAnalyser(), SearchTerm);
        _englishQueryTerm  = AnalyseQueryTerm(new EnglishAnalyser(),  SearchTerm);

        _standardIndexPath = BuildIndex(paragraphs, new StandardAnalyser(), "standard");
        _englishIndexPath  = BuildIndex(paragraphs, new EnglishAnalyser(),  "english");

        _standardDirectory = new MMapDirectory(_standardIndexPath);
        _englishDirectory  = new MMapDirectory(_englishIndexPath);

        _standardSearcher = new IndexSearcher(_standardDirectory);
        _englishSearcher  = new IndexSearcher(_englishDirectory);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _standardSearcher?.Dispose();
        _englishSearcher?.Dispose();

        _standardDirectory = null;
        _englishDirectory  = null;

        DeleteDirectory(_standardIndexPath);
        DeleteDirectory(_englishIndexPath);
    }

    /// <summary>
    /// Searches the standard-analyser index with a non-stemmed query term.
    /// </summary>
    [Benchmark(Baseline = true)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public int Standard_Search()
    {
        var results = _standardSearcher!.Search(new TermQuery("body", _standardQueryTerm), TopN);
        return results.TotalHits;
    }

    /// <summary>
    /// Searches the English-analyser index with a Porter-stemmed query term.
    /// </summary>
    [Benchmark]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public int English_Search()
    {
        var results = _englishSearcher!.Search(new TermQuery("body", _englishQueryTerm), TopN);
        return results.TotalHits;
    }

    private static string AnalyseQueryTerm(IAnalyser analyser, string term)
    {
        var tokens = analyser.Analyse(term.AsSpan());
        return tokens.Count > 0 ? tokens[0].Text : term;
    }

    private static string BuildIndex(BookParagraph[] paragraphs, IAnalyser analyser, string label)
    {
        var path = Path.Combine(Path.GetTempPath(), $"leanlucene-realdata-{label}-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);

        var directory = new MMapDirectory(path);
        using (var writer = new IndexWriter(directory, new IndexWriterConfig
        {
            DefaultAnalyser = analyser,
            MaxBufferedDocs = 10_000,
            RamBufferSizeMB = 256,
            DurableCommits = false
        }))
        {
            foreach (var para in paragraphs)
            {
                var doc = new LeanDocument();
                doc.Add(new StringField("id", para.Id));
                doc.Add(new StringField("title", para.Title));
                doc.Add(new TextField("body", para.Body));
                writer.AddDocument(doc);
            }

            writer.Commit();
        }

        return path;
    }

    private static void DeleteDirectory(string path)
    {
        if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path))
            Directory.Delete(path, recursive: true);
    }
}
