using Rowles.LeanLucene.Analysis;

namespace Rowles.LeanLucene.Benchmarks.Quick.Benchmarks;

/// <summary>
/// Sanity benchmark: tokenises 1,000 documents through the standard analysis pipeline.
/// Verifies that analysis throughput has not regressed.
/// </summary>
internal sealed class AnalysisSanityBenchmark : IQuickBenchmark
{
    private const int DocumentCount = 1_000;

    public string Name => "Analysis.Tokenise1000Docs";

    private string[] _documents = [];
    private StandardAnalyser _analyser = null!;

    public void Setup()
    {
        _documents = SanityBenchmarkData.BuildDocuments(DocumentCount);
        _analyser = new StandardAnalyser();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void Run()
    {
        int totalTokens = 0;
        for (int i = 0; i < _documents.Length; i++)
        {
            var tokens = _analyser.Analyse(_documents[i].AsSpan());
            totalTokens += tokens.Count;
        }

        // Prevent dead-code elimination.
        if (totalTokens == 0)
            throw new InvalidOperationException("Expected tokens from analysis.");
    }

    public void Cleanup()
    {
        _documents = [];
    }
}
