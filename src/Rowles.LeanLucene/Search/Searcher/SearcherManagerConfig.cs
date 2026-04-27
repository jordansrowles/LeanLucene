using Rowles.LeanLucene.Store;

using Rowles.LeanLucene.Search.Simd;
using Rowles.LeanLucene.Search.Parsing;
using Rowles.LeanLucene.Search.Highlighting;
namespace Rowles.LeanLucene.Search.Searcher;

/// <summary>
/// Configuration for <see cref="SearcherManager"/>.
/// </summary>
public sealed class SearcherManagerConfig
{
    /// <summary>How often to poll for new commits. Default: 1 second.</summary>
    public TimeSpan RefreshInterval { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>Searcher configuration applied to each newly opened IndexSearcher.</summary>
    public IndexSearcherConfig SearcherConfig { get; set; } = new();
}
