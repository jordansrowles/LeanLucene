# Validation and recovery

## Validate an index

`IndexValidator.Validate` checks that the latest commit's segment files are
present and internally consistent.

```csharp
using Rowles.LeanLucene.Index;
using Rowles.LeanLucene.Store;

using var dir = new MMapDirectory("./index");
IndexCheckResult result = IndexValidator.Validate(dir);

if (!result.IsHealthy)
    foreach (var issue in result.Issues)
        Console.Error.WriteLine(issue);

Console.WriteLine($"Segments checked: {result.SegmentsChecked}");
Console.WriteLine($"Documents checked: {result.DocumentsChecked}");
```

## Crash recovery

`IndexRecovery.RecoverLatestCommit` finds the newest valid commit, falling back to
older generations if the latest is corrupt. It also cleans up orphaned segment
files and stale temp files left behind by an interrupted commit.

```csharp
var commit = IndexRecovery.RecoverLatestCommit("./index", cleanupOrphans: true);
if (commit is null)
    Console.WriteLine("No valid commit; index is empty or unrecoverable.");
```

`IndexWriter` runs writer-side recovery on open. Reader-side polling
(via `SearcherManager`) calls it with `cleanupOrphans: false`.

## See also

- <xref:Rowles.LeanLucene.Index.IndexValidator>
- <xref:Rowles.LeanLucene.Index.IndexRecovery>
- <xref:Rowles.LeanLucene.Index.IndexCheckResult>
