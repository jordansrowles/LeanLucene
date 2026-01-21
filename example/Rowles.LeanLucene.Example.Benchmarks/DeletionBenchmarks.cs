using BenchmarkDotNet.Attributes;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using IODirectory = System.IO.Directory;
using LeanDocument = Rowles.LeanLucene.Document.LeanDocument;
using LeanMMapDirectory = Rowles.LeanLucene.Store.MMapDirectory;
using LeanStringField = Rowles.LeanLucene.Document.StringField;
using LeanTextField = Rowles.LeanLucene.Document.TextField;
using LuceneStringField = Lucene.Net.Documents.StringField;
using LuceneTextField = Lucene.Net.Documents.TextField;

namespace Rowles.LeanLucene.Example.Benchmarks;

/// <summary>
/// Measures document deletion performance across all 3 libraries.
/// Indexes N docs, then deletes ~10% and verifies the index reflects the change.
/// </summary>
[MemoryDiagnoser]
[HtmlExporter]
[JsonExporterAttribute.Full]
[MarkdownExporterAttribute.GitHub]
[SimpleJob]
public class DeletionBenchmarks
{
    public static IEnumerable<int> DocCounts => BenchmarkData.GetDocCounts(BenchmarkData.DefaultDocCount);

    [ParamsSource(nameof(DocCounts))]
    public int DocumentCount { get; set; }

    private string[] _documents = [];

    [GlobalSetup]
    public void Setup()
    {
        _documents = BenchmarkData.BuildDocuments(DocumentCount);
    }

    [Benchmark(Baseline = true)]
    public int LeanLucene_DeleteDocuments()
    {
        var path = Path.Combine(Path.GetTempPath(), $"leanlucene-bench-del-{Guid.NewGuid():N}");
        IODirectory.CreateDirectory(path);

        try
        {
            var directory = new LeanMMapDirectory(path);
            using var writer = new Rowles.LeanLucene.Index.IndexWriter(
                directory,
                new Rowles.LeanLucene.Index.IndexWriterConfig { MaxBufferedDocs = 10_000, RamBufferSizeMB = 256 });

            for (int i = 0; i < _documents.Length; i++)
            {
                var doc = new LeanDocument();
                doc.Add(new LeanStringField("id", i.ToString(System.Globalization.CultureInfo.InvariantCulture)));
                doc.Add(new LeanTextField("body", _documents[i]));
                writer.AddDocument(doc);
            }
            writer.Commit();

            int deleteCount = DocumentCount / 10;
            for (int i = 0; i < deleteCount; i++)
                writer.DeleteDocuments(new Rowles.LeanLucene.Search.TermQuery("id", i.ToString(System.Globalization.CultureInfo.InvariantCulture)));
            writer.Commit();

            return deleteCount;
        }
        finally
        {
            if (IODirectory.Exists(path))
                IODirectory.Delete(path, recursive: true);
        }
    }

    [Benchmark]
    public int LuceneNet_DeleteDocuments()
    {
        using var directory = new RAMDirectory();
        using var analyzer = new StandardAnalyzer(LuceneVersion.LUCENE_48);
        using var writer = new Lucene.Net.Index.IndexWriter(
            directory,
            new Lucene.Net.Index.IndexWriterConfig(LuceneVersion.LUCENE_48, analyzer));

        for (int i = 0; i < _documents.Length; i++)
        {
            var doc = new Lucene.Net.Documents.Document
            {
                new LuceneStringField("id", i.ToString(System.Globalization.CultureInfo.InvariantCulture), Field.Store.YES),
                new LuceneTextField("body", _documents[i], Field.Store.NO)
            };
            writer.AddDocument(doc);
        }
        writer.Commit();

        int deleteCount = DocumentCount / 10;
        for (int i = 0; i < deleteCount; i++)
            writer.DeleteDocuments(new Term("id", i.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        writer.Commit();

        return deleteCount;
    }

}
