namespace Rowles.LeanLucene.Search.Searcher;

/// <summary>
/// Thread-safe LRU query result cache. Entries are keyed by (Query, topN) and
/// invalidated when the commit generation changes.
/// </summary>
/// <remarks>
/// <para>
/// Invalidation contract: the cache is keyed against a single commit generation.
/// Callers must invoke <see cref="Invalidate"/> (or assign a fresh cache) whenever
/// the underlying searcher is swapped to a newer commit, otherwise stale results
/// may be returned. Cached results are <em>snapshot views</em>: doc IDs and scores
/// reflect the segments visible at the moment of caching, and remain valid for as
/// long as those segments are still referenced by the live searcher.
/// </para>
/// <para>
/// Recommended placement: hold one cache per <see cref="SearcherManager"/> rather
/// than per <see cref="IndexSearcher"/>, and call <see cref="Invalidate"/> from the
/// refresh hook. This avoids a write race where two concurrent searches racing
/// against a commit could publish results from differing generations.
/// </para>
/// </remarks>
public sealed class QueryCache
{
    private readonly int _maxEntries;
    private readonly Lock _lock = new();
    private readonly Dictionary<CacheKey, LinkedListNode<CacheEntry>> _map;
    private readonly LinkedList<CacheEntry> _lru = new();
    private long _generation;
    private long _hits;
    private long _misses;    /// <summary>
    /// Initialises a new <see cref="QueryCache"/> with the specified maximum entry count.
    /// </summary>
    /// <param name="maxEntries">The maximum number of entries to hold. Must be at least 1.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="maxEntries"/> is less than 1.</exception>
    public QueryCache(int maxEntries = 1024)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxEntries, 1);
        _maxEntries = maxEntries;
        _map = new(maxEntries);
    }

    /// <summary>Total cache hits since creation.</summary>
    public long Hits { get { lock (_lock) return _hits; } }

    /// <summary>Total cache misses since creation.</summary>
    public long Misses { get { lock (_lock) return _misses; } }

    /// <summary>Current number of cached entries.</summary>
    public int Count
    {
        get { lock (_lock) return _map.Count; }
    }

    /// <summary>
    /// Tries to retrieve a cached result. Returns null on miss.
    /// </summary>
    public TopDocs? TryGet(Query query, int topN)
    {
        var key = new CacheKey(query, topN);
        lock (_lock)
        {
            if (_map.TryGetValue(key, out var node) && node.Value.Generation == _generation)
            {
                // Move to front (most recently used)
                _lru.Remove(node);
                _lru.AddFirst(node);
                _hits++;
                return node.Value.Result;
            }

            // Stale entry — remove it
            if (node is not null)
            {
                _lru.Remove(node);
                _map.Remove(key);
            }

            _misses++;
            return null;
        }
    }

    /// <summary>
    /// Stores a query result in the cache.
    /// </summary>
    public void Put(Query query, int topN, TopDocs result)
    {
        var key = new CacheKey(query, topN);
        var entry = new CacheEntry(key, result, _generation);

        lock (_lock)
        {
            if (_map.TryGetValue(key, out var existing))
            {
                _lru.Remove(existing);
                _map.Remove(key);
            }

            var node = _lru.AddFirst(entry);
            _map[key] = node;

            // Evict LRU entries if over capacity
            while (_map.Count > _maxEntries)
            {
                var last = _lru.Last!;
                _map.Remove(last.Value.Key);
                _lru.RemoveLast();
            }
        }
    }

    /// <summary>
    /// Invalidates all cached entries by bumping the generation.
    /// Lazy invalidation: stale entries are removed on next access.
    /// </summary>
    public void Invalidate()
    {
        lock (_lock)
        {
            _generation++;
        }
    }

    /// <summary>Clears all entries and resets counters.</summary>
    public void Clear()
    {
        lock (_lock)
        {
            _map.Clear();
            _lru.Clear();
            _generation++;
            _hits = 0;
            _misses = 0;
        }
    }

    private readonly record struct CacheKey(Query Query, int TopN)
    {
        public bool Equals(CacheKey other) =>
            TopN == other.TopN && Query.Equals(other.Query);

        public override int GetHashCode() => HashCode.Combine(Query, TopN);
    }

    private sealed class CacheEntry(CacheKey key, TopDocs result, long generation)
    {
        public CacheKey Key { get; } = key;
        public TopDocs Result { get; } = result;
        public long Generation { get; } = generation;
    }
}
