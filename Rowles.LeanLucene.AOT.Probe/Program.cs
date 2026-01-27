// AOT probe entry point — exercises the main library surface to trigger
// ILLink/NativeAOT trim/AOT analysis warnings at publish time.
using Rowles.LeanLucene.Analysis;
using Rowles.LeanLucene.Document;
using Rowles.LeanLucene.Document.Fields;
using Rowles.LeanLucene.Index;
using Rowles.LeanLucene.Search;
using Rowles.LeanLucene.Store;

var indexPath = Path.Combine(Path.GetTempPath(), "leanlucene-aot-probe");
if (Directory.Exists(indexPath)) Directory.Delete(indexPath, recursive: true);
Directory.CreateDirectory(indexPath);

var dir = new MMapDirectory(indexPath);

using (var writer = new IndexWriter(dir, new IndexWriterConfig()))
{
    var doc = new LeanDocument();
    doc.Add(new TextField("body", "hello world native aot probe"));
    doc.Add(new StringField("id", "probe-1"));
    doc.Add(new NumericField("score", 42.0));
    doc.Add(new VectorField("vec", new ReadOnlyMemory<float>([0.1f, 0.2f, 0.3f, 0.4f])));
    writer.AddDocument(doc);
    writer.Commit();
}

using var searcher = new IndexSearcher(dir);

// BooleanQuery
var bq = new BooleanQuery();
bq.Add(new TermQuery("body", "hello"), Occur.Must);
bq.Add(new TermQuery("body", "world"), Occur.Should);
var r1 = searcher.Search(bq, topN: 5);

// PhraseQuery
var r2 = searcher.Search(new PhraseQuery("body", "hello", "world"), topN: 5);

// FuzzyQuery
var r3 = searcher.Search(new FuzzyQuery("body", "helo", maxEdits: 1), topN: 5);

// WildcardQuery
var r4 = searcher.Search(new WildcardQuery("body", "hel*"), topN: 5);

// RegexpQuery — uses Regex.Compiled which has AOT implications
var r5 = searcher.Search(new RegexpQuery("body", "hel+o"), topN: 5);

// RangeQuery
var r6 = searcher.Search(new RangeQuery("score", 0, 100), topN: 5);

// VectorQuery
var r7 = searcher.Search(new VectorQuery("vec", [0.1f, 0.2f, 0.3f, 0.4f], topK: 3), topN: 3);

// FunctionScoreQuery
var r8 = searcher.Search(new FunctionScoreQuery(new TermQuery("body", "hello"), "score", ScoreMode.Multiply), topN: 5);

// QueryParser
var analyser = new StandardAnalyser();
var qp = new QueryParser("body", analyser);
var r9 = searcher.Search(qp.Parse("hello world"), topN: 5);

Console.WriteLine($"AOT probe complete. TotalHits={r1.TotalHits}");

if (Directory.Exists(indexPath)) Directory.Delete(indexPath, recursive: true);
