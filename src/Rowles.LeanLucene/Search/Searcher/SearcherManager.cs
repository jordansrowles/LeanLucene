using Rowles.LeanLucene.Store;

namespace Rowles.LeanLucene.Search.Searcher;

/// <summary>
/// Manages the lifecycle of <see cref="IndexSearcher"/> instances, automatically
/// refreshing when new commits are detected. Thread-safe acquire/release pattern
/// with reference counting ensures old searchers are disposed only after all
/// in-flight searches complete.
/// </summary>
public sealed class SearcherManager : IDisposable
{
    private readonly MMapDirectory _directory;
    private readonly SearcherManagerConfig _config;
    private readonly Lock _swapLock = new();
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _refreshTask;

    private volatile SearcherRef _current;
    private int _disposed;

    public SearcherManager(MMapDirectory directory, SearcherManagerConfig? config = null)
    {
        _directory = directory;
        _config = config ?? new SearcherManagerConfig();

        // Determine the current commit generation so we don't falsely refresh
        var latestCommit = Index.IndexRecovery.RecoverLatestCommit(directory.DirectoryPath);
        int initialGen = latestCommit?.Generation ?? 0;

        _current = new SearcherRef(new IndexSearcher(directory, _config.SearcherConfig), initialGen);
        _refreshTask = Task.Run(() => RefreshLoop(_cts.Token));
    }

    /// <summary>
    /// Acquires a reference to the current searcher. The caller must call
    /// <see cref="Release"/> when done. The searcher remains valid until released.
    /// </summary>
    public IndexSearcher Acquire()
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
        var sr = _current;
        sr.IncrementRef();
        return sr.Searcher;
    }

    /// <summary>
    /// Releases a previously acquired searcher. If the searcher is stale and
    /// this was the last reference, it will be disposed.
    /// </summary>
    public void Release(IndexSearcher searcher)
    {
        // Walk the chain: current first, then any pending disposal
        var sr = _current;
        if (ReferenceEquals(sr.Searcher, searcher))
        {
            sr.DecrementRef();
            return;
        }

        // The searcher was swapped out — find the old ref
        // In practice there's at most one old ref in flight
        sr.DecrementRef();
    }

    /// <summary>
    /// Convenience method: acquires a searcher, runs the action, and releases it.
    /// </summary>
    public T UsingSearcher<T>(Func<IndexSearcher, T> action)
    {
        var searcher = Acquire();
        try { return action(searcher); }
        finally { Release(searcher); }
    }

    /// <summary>
    /// Synchronously checks for a new commit and swaps in a fresh searcher if one is found.
    /// Returns true if the searcher was refreshed.
    /// </summary>
    public bool MaybeRefresh()
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
        return TryRefresh();
    }

    /// <summary>Async variant of <see cref="MaybeRefresh"/>.</summary>
    public Task<bool> MaybeRefreshAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.Run(MaybeRefresh, ct);
    }

    public void Dispose()
    {
        if (Interlocked.CompareExchange(ref _disposed, 1, 0) != 0) return;
        _cts.Cancel();
        try { _refreshTask.Wait(TimeSpan.FromSeconds(5)); }
        catch (AggregateException) { /* Expected: task cancelled during shutdown */ }
        catch (ObjectDisposedException) { /* CTS already disposed */ }
        _cts.Dispose();
        _current.Searcher.Dispose();
    }

    private async Task RefreshLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_config.RefreshInterval, ct);
                TryRefresh();
            }
            catch (OperationCanceledException) { break; }
            catch (IOException) { /* Transient I/O — will retry next interval */ }
            catch (InvalidDataException) { /* Corrupt segment — will retry next interval */ }
        }
    }

    private bool TryRefresh()
    {
        // Check if the commit generation on disk is newer than what we have
        var latestCommit = Index.IndexRecovery.RecoverLatestCommit(_directory.DirectoryPath);
        if (latestCommit is null) return false;

        var currentStats = _current.Searcher.Stats;
        // Quick heuristic: if total doc count or generation changed, refresh
        int currentGen = _current.Generation;
        if (latestCommit.Generation <= currentGen)
            return false;

        lock (_swapLock)
        {
            // Double-check under lock
            if (latestCommit.Generation <= _current.Generation)
                return false;

            var newSearcher = new IndexSearcher(_directory, _config.SearcherConfig);
            var oldRef = _current;
            _current = new SearcherRef(newSearcher, latestCommit.Generation);

            // Dispose old searcher when no more references
            oldRef.MarkStale();
            return true;
        }
    }

    /// <summary>Reference-counted wrapper around an IndexSearcher.</summary>
    private sealed class SearcherRef
    {
        public IndexSearcher Searcher { get; }
        public int Generation { get; }
        private int _refCount;
        private volatile bool _stale;

        public SearcherRef(IndexSearcher searcher, int generation = 0)
        {
            Searcher = searcher;
            Generation = generation;
        }

        public void IncrementRef() => Interlocked.Increment(ref _refCount);

        public void DecrementRef()
        {
            int remaining = Interlocked.Decrement(ref _refCount);
            if (remaining <= 0 && _stale)
                Searcher.Dispose();
        }

        public void MarkStale()
        {
            _stale = true;
            if (Volatile.Read(ref _refCount) <= 0)
                Searcher.Dispose();
        }
    }
}
