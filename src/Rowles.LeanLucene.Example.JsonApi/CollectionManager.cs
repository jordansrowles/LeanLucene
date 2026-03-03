using Rowles.LeanLucene.Index.Indexer;
using Rowles.LeanLucene.Search.Searcher;
using Rowles.LeanLucene.Store;

namespace Rowles.LeanLucene.Example.JsonApi;

/// <summary>
/// Manages a set of named collections, each backed by its own
/// <see cref="MMapDirectory"/> on disk and a dedicated <see cref="IndexWriter"/>
/// + <see cref="IndexSearcher"/> pair.
/// </summary>
public sealed class CollectionManager : IDisposable
{
    private sealed class CollectionState(IndexWriter writer, MMapDirectory dir) : IDisposable
    {
        public readonly IndexWriter Writer = writer;
        public readonly MMapDirectory Dir = dir;
        private IndexSearcher? _searcher;
        private volatile bool _needsRefresh = true;

        public IndexSearcher GetOrRefreshSearcher()
        {
            if (_needsRefresh || _searcher is null)
            {
                _searcher?.Dispose();
                _searcher = new IndexSearcher(Dir);
                _needsRefresh = false;
            }
            return _searcher;
        }

        public void InvalidateSearcher() => _needsRefresh = true;

        public void Dispose()
        {
            _searcher?.Dispose();
            Writer.Dispose();
            // MMapDirectory is not IDisposable; handles its own cleanup
        }
    }

    private readonly string _rootPath;
    private readonly Dictionary<string, CollectionState> _collections = new(StringComparer.Ordinal);
    private readonly Lock _lock = new();

    public CollectionManager(string rootPath)
    {
        _rootPath = rootPath;
        Directory.CreateDirectory(rootPath);

        // Re-open any previously persisted collections
        foreach (var dir in Directory.EnumerateDirectories(rootPath))
        {
            string name = Path.GetFileName(dir);
            try { OpenOrCreate(name); }
            catch { /* skip corrupt collection on startup */ }
        }
    }

    public IEnumerable<(string Name, int DocCount)> ListCollections()
    {
        List<(string, int)> result;
        lock (_lock)
        {
            result = _collections
                .Select(kv => (kv.Key, kv.Value.GetOrRefreshSearcher().Stats.LiveDocCount))
                .ToList();
        }
        return result;
    }

    public IndexWriter GetWriter(string name)
    {
        lock (_lock) { return OpenOrCreate(name).Writer; }
    }

    public IndexSearcher GetSearcher(string name)
    {
        lock (_lock) { return OpenOrCreate(name).GetOrRefreshSearcher(); }
    }

    public bool DropCollection(string name)
    {
        lock (_lock)
        {
            if (!_collections.TryGetValue(name, out var state))
                return false;

            state.Dispose();
            _collections.Remove(name);
            string colPath = CollectionPath(name);
            if (Directory.Exists(colPath))
                Directory.Delete(colPath, recursive: true);
            return true;
        }
    }

    public bool Exists(string name)
    {
        lock (_lock) { return _collections.ContainsKey(name); }
    }

    public void CommitAndRefresh(string name)
    {
        CollectionState state;
        lock (_lock) { state = OpenOrCreate(name); }
        state.Writer.Commit();
        state.InvalidateSearcher();
    }

    private CollectionState OpenOrCreate(string name)
    {
        if (_collections.TryGetValue(name, out var existing))
            return existing;

        string path = CollectionPath(name);
        Directory.CreateDirectory(path);
        var dir = new MMapDirectory(path);
        var writer = new IndexWriter(dir, new IndexWriterConfig());
        var state = new CollectionState(writer, dir);
        _collections[name] = state;
        return state;
    }

    private string CollectionPath(string name) => Path.Combine(_rootPath, name);

    public void Dispose()
    {
        lock (_lock)
        {
            foreach (var state in _collections.Values)
                state.Dispose();
            _collections.Clear();
        }
    }
}
