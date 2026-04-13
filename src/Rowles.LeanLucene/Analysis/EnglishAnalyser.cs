using Rowles.LeanLucene.Analysis.Filters;
using Rowles.LeanLucene.Analysis.Tokenisers;

namespace Rowles.LeanLucene.Analysis;

/// <summary>
/// English analysis pipeline with stemming for improved recall.
/// Pipeline: tokenise → lowercase → stop-word removal → Porter stem.
/// </summary>
/// <remarks>
/// Compared to <see cref="StandardAnalyser"/>, this pipeline reduces tokens to their
/// root forms (e.g. "running", "runs", "ran" all become "run"), so a single query
/// term matches all morphological variants. The trade-off is slightly higher indexing
/// cost and reduced precision when exact-form matching is required.
///
/// Thread-safety: each instance is not thread-safe. Create one instance per thread,
/// or use separate instances as <see cref="IndexWriter"/> does in concurrent mode.
/// </remarks>
public sealed class EnglishAnalyser : IAnalyser
{
    private readonly Analyser _pipeline = new(
        new Tokeniser(),
        new LowercaseFilter(),
        new StopWordFilter(),
        new PorterStemmerFilter());

    /// <inheritdoc/>
    public List<Token> Analyse(ReadOnlySpan<char> input) => _pipeline.Analyse(input);
}
