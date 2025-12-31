using BenchmarkDotNet.Attributes;
using Rowles.LeanLucene.Analysis;

namespace Rowles.LeanLucene.Example.Benchmarks;

/// <summary>
/// Measures analysis pipeline throughput: tokenisation + lowercase + stop-word removal.
/// </summary>
[MemoryDiagnoser]
[SimpleJob]
public class AnalysisBenchmarks
{
    [Params(1_000)]
    public int DocumentCount { get; set; }

    private string[] _documents = [];
    private StandardAnalyser _analyser = null!;

    [GlobalSetup]
    public void Setup()
    {
        _documents = BenchmarkData.BuildDocuments(DocumentCount);
        _analyser = new StandardAnalyser();
    }

    [Benchmark]
    public int LeanLucene_Analyse()
    {
        int totalTokens = 0;
        for (int i = 0; i < _documents.Length; i++)
        {
            var tokens = _analyser.Analyse(_documents[i].AsSpan());
            totalTokens += tokens.Count;
        }
        return totalTokens;
    }
}
