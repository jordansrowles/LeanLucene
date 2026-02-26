namespace Rowles.LeanLucene.Search;

/// <summary>
/// Per-query options controlling resource usage and result delivery.
/// Pass to IndexSearcher.Search() overloads to limit memory and enable streaming.
/// </summary>
public sealed class SearchOptions
{
    /// <summary>
    /// Maximum estimated bytes this query is allowed to allocate for intermediate
    /// structures (posting lists, score buffers, etc.). Default: no limit (long.MaxValue).
    /// When exceeded, search terminates early and returns partial results.
    /// </summary>
    public long MaxResultBytes { get; init; } = long.MaxValue;

    /// <summary>
    /// When true, results are yielded as they are found rather than collected
    /// into a complete TopDocs. Useful for large result sets where the caller
    /// processes documents one-by-one. Default: false.
    /// </summary>
    public bool StreamResults { get; init; }

    /// <summary>
    /// Maximum wall-clock time for this query. Default: no limit.
    /// When exceeded, search terminates early and returns partial results.
    /// </summary>
    public TimeSpan? Timeout { get; init; }

    /// <summary>
    /// Default options with no limits.
    /// </summary>
    public static SearchOptions Default { get; } = new();

    /// <summary>
    /// Creates options with a specific memory budget.
    /// </summary>
    public static SearchOptions WithBudget(long maxBytes) => new() { MaxResultBytes = maxBytes };

    /// <summary>
    /// Creates options with a specific timeout.
    /// </summary>
    public static SearchOptions WithTimeout(TimeSpan timeout) => new() { Timeout = timeout };
}
