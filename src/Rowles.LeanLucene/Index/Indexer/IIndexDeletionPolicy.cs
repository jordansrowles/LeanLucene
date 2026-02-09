namespace Rowles.LeanLucene.Index.Indexer;

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
