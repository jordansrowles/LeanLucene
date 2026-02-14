namespace Rowles.LeanLucene.Index.Indexer;

/// <summary>Keeps only the latest commit, deleting all older segments_N and stats_N files.</summary>
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
        // Prune old stats files
        foreach (var file in Directory.GetFiles(directoryPath, "stats_*.json"))
        {
            var name = Path.GetFileNameWithoutExtension(file); // "stats_N"
            if (name.StartsWith("stats_") &&
                int.TryParse(name["stats_".Length..], out int gen) &&
                gen < currentGeneration)
            {
                try { File.Delete(file); } catch { /* best-effort */ }
            }
        }
    }
}
