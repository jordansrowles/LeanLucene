using BenchmarkDotNet.Attributes;
using Rowles.LeanLucene.Index;
using Rowles.LeanLucene.Search;
using LeanDocument = Rowles.LeanLucene.Document.LeanDocument;
using LeanMMapDirectory = Rowles.LeanLucene.Store.MMapDirectory;
using LeanStringField = Rowles.LeanLucene.Document.Fields.StringField;
using LeanTextField = Rowles.LeanLucene.Document.Fields.TextField;

namespace Rowles.LeanLucene.Benchmarks;

/// <summary>
/// Measures full roundtrip: index 100 docs + TermQuery. Tests startup/small-scale overhead.
/// </summary>
[MemoryDiagnoser]
[HtmlExporter]
[JsonExporterAttribute.Full]
[MarkdownExporterAttribute.GitHub]
[RPlotExporter]
[SimpleJob]
public class SmallIndexBenchmarks
{
    private const int TopN = 10;

    public static IEnumerable<int> DocCounts => BenchmarkData.GetDocCounts(100);

    [ParamsSource(nameof(DocCounts))]
    public int DocumentCount { get; set; }

    private string[] _documents = [];

    [GlobalSetup]
    public void Setup()
    {
        _documents = BenchmarkData.BuildDocuments(DocumentCount);
    }

    [Benchmark]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public int IndexAndQuery_Roundtrip()
    {
        var path = Path.Combine(Path.GetTempPath(), $"leanlucene-bench-small-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);

        try
        {
            var directory = new LeanMMapDirectory(path);
            using (var writer = new IndexWriter(
                directory,
                new IndexWriterConfig { MaxBufferedDocs = 128, RamBufferSizeMB = 16 }))
            {
                for (int i = 0; i < _documents.Length; i++)
                {
                    var doc = new LeanDocument();
                    doc.Add(new LeanStringField("id", i.ToString(System.Globalization.CultureInfo.InvariantCulture)));
                    doc.Add(new LeanTextField("body", _documents[i]));
                    writer.AddDocument(doc);
                }
                writer.Commit();
            }

            using var searcher = new IndexSearcher(directory);
            var topDocs = searcher.Search(new TermQuery("body", "search"), TopN);
            return topDocs.TotalHits;
        }
        finally
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
    }
}
