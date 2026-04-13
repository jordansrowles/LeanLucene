namespace Rowles.LeanLucene.Benchmarks.RealData;

/// <summary>
/// Measures index build time on real Project Gutenberg ebook paragraphs.
/// Each iteration creates a fresh temporary index, indexes all paragraphs, commits, and cleans up.
/// Compares <see cref="StandardAnalyser"/> against <see cref="EnglishAnalyser"/>.
/// </summary>
[MemoryDiagnoser]
[HtmlExporter]
[JsonExporterAttribute.Full]
[MarkdownExporterAttribute.GitHub]
[SimpleJob]
public class GutenbergIndexingBenchmarks
{
    private BookParagraph[] _paragraphs = [];

    [GlobalSetup]
    public void Setup() => _paragraphs = GutenbergDataLoader.Load();

    [GlobalCleanup]
    public void Cleanup() => _paragraphs = [];

    /// <summary>
    /// Indexes all paragraphs using the standard analyser (tokenise+lowercase+stopwords).
    /// </summary>
    [Benchmark(Baseline = true)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public int Standard_Index() => RunIndex(new StandardAnalyser());

    /// <summary>
    /// Indexes all paragraphs using the English analyser (tokenise+lowercase+stopwords+Porter stem).
    /// </summary>
    [Benchmark]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public int English_Index() => RunIndex(new EnglishAnalyser());

    private int RunIndex(IAnalyser analyser)
    {
        var path = Path.Combine(Path.GetTempPath(), $"leanlucene-realdata-idx-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);

        try
        {
            var directory = new MMapDirectory(path);
            using var writer = new IndexWriter(directory, new IndexWriterConfig
            {
                DefaultAnalyser = analyser,
                MaxBufferedDocs = 10_000,
                RamBufferSizeMB = 256,
                DurableCommits = false
            });

            foreach (var para in _paragraphs)
            {
                var doc = new LeanDocument();
                doc.Add(new StringField("id", para.Id));
                doc.Add(new StringField("title", para.Title));
                doc.Add(new TextField("body", para.Body));
                writer.AddDocument(doc);
            }

            writer.Commit();
            return _paragraphs.Length;
        }
        finally
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
    }
}
