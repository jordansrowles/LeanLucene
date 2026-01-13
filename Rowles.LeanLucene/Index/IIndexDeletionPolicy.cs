namespace Rowles.LeanLucene.Index;

/// <summary>
/// Controls which old commit files and their referenced segments are pruned after a new commit.
/// </summary>
public interface IIndexDeletionPolicy
{
    /// <summary>
    /// Called after a new commit is written. The implementation should delete
    /// any commit files (and their segments) that are no longer needed.
    /// </summary>
    void OnCommit(string directoryPath, int currentGeneration);
}

/// <summary>Keeps only the latest commit, deleting all older segments_N files.</summary>
public sealed class KeepLatestCommitPolicy : IIndexDeletionPolicy
{
    public void OnCommit(string directoryPath, int currentGeneration)
    {
        foreach (var file in Directory.GetFiles(directoryPath, "segments_*"))
        {
            var name = Path.GetFileName(file);
            if (name.StartsWith("segments_") &&
                int.TryParse(name["segments_".Length..], out int gen) &&
                gen < currentGeneration)
            {
                try { File.Delete(file); } catch { /* best-effort */ }
            }
        }
    }
}

/// <summary>Keeps the last N commit generations, deleting older ones.</summary>
public sealed class KeepLastNCommitsPolicy : IIndexDeletionPolicy
{
    private readonly int _maxCommits;

    public KeepLastNCommitsPolicy(int maxCommits)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxCommits, 1);
        _maxCommits = maxCommits;
    }

    public void OnCommit(string directoryPath, int currentGeneration)
    {
        int threshold = currentGeneration - _maxCommits;
        if (threshold <= 0) return;

        foreach (var file in Directory.GetFiles(directoryPath, "segments_*"))
        {
            var name = Path.GetFileName(file);
            if (name.StartsWith("segments_") &&
                int.TryParse(name["segments_".Length..], out int gen) &&
                gen <= threshold)
            {
                try { File.Delete(file); } catch { /* best-effort */ }
            }
        }
    }
}
