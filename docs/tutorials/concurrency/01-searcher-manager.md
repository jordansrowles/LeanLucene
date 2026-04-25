# Searcher manager

`SearcherManager` keeps a current `IndexSearcher` open and swaps in a fresh one
when a new commit is detected. Use it to share a searcher across many concurrent
queries.

## Set up

```csharp
using Rowles.LeanLucene.Search.Searcher;
using Rowles.LeanLucene.Store;

using var dir = new MMapDirectory("./index");
using var manager = new SearcherManager(dir, new SearcherManagerConfig
{
    RefreshInterval = TimeSpan.FromSeconds(1),
    SearcherConfig  = new IndexSearcherConfig { EnableQueryCache = true },
});
```

A background loop polls for new commits at `RefreshInterval`.

## Acquire and release

```csharp
var searcher = manager.Acquire();
try
{
    var hits = searcher.Search(query, 10);
}
finally
{
    manager.Release(searcher);
}
```

Or use the convenience method:

```csharp
var hits = manager.UsingSearcher(s => s.Search(query, 10));
```

## Forcing a refresh

```csharp
bool refreshed = manager.MaybeRefresh();          // sync
bool refreshedAsync = await manager.MaybeRefreshAsync();
```

Returns `true` when a newer commit was loaded.

## See also

- <xref:Rowles.LeanLucene.Search.Searcher.SearcherManager>
- <xref:Rowles.LeanLucene.Search.Searcher.SearcherManagerConfig>
