using System.Text.Json;

namespace Rowles.LeanLucene.Index;

/// <summary>
/// Lightweight crash recovery for LeanLucene indices.
/// On startup: validates the latest commit, falls back to prior generations on corruption,
/// and cleans up orphaned segment files and temp files.
/// </summary>
public static class IndexRecovery
{
    /// <summary>Known segment file extensions that should be cleaned up when orphaned.</summary>
    private static readonly string[] SegmentExtensions =
        [".seg", ".dic", ".pos", ".nrm", ".fdt", ".fdx", ".del",
         ".dvn", ".dvs", ".bkd", ".vec", ".tvd", ".tvx", ".num"];

    /// <summary>
    /// Attempts to load the latest valid commit from the index directory.
    /// Tries generations from highest to lowest. Returns null if no valid commit exists.
    /// </summary>
    /// <param name="directoryPath">The index directory.</param>
    /// <param name="cleanupOrphans">
    /// When <c>true</c> (writer-side recovery), deletes orphan segment files and stale temp files.
    /// When <c>false</c> (reader-side polling), only inspects the directory and never mutates it —
    /// reader threads must not race the writer by deleting in-flight segment files.
    /// </param>
    public static RecoveryResult? RecoverLatestCommit(string directoryPath, bool cleanupOrphans = true)
    {
        // Clean up any leftover temp files from interrupted commits (writer-side only).
        if (cleanupOrphans)
            CleanupTempFiles(directoryPath);

        var commitFiles = FindCommitFiles(directoryPath);
        if (commitFiles.Count == 0)
            return null;

        // Try each commit from newest to oldest
        foreach (var (generation, filePath) in commitFiles)
        {
            var result = TryLoadCommit(directoryPath, filePath, generation);
            if (result is not null)
            {
                if (cleanupOrphans)
                    CleanupOrphanedSegments(directoryPath, result.SegmentIds);
                return result;
            }
        }

        return null;
    }

    /// <summary>
    /// Enumerates all segments_N files, sorted by generation descending.
    /// </summary>
    private static List<(int Generation, string FilePath)> FindCommitFiles(string directoryPath)
    {
        var result = new List<(int, string)>();
        if (!Directory.Exists(directoryPath))
            return result;

        foreach (var file in Directory.GetFiles(directoryPath, "segments_*"))
        {
            var fileName = Path.GetFileName(file);
            // Skip temp files
            if (fileName.EndsWith(".tmp", StringComparison.Ordinal))
                continue;
            var genStr = fileName.AsSpan("segments_".Length);
            if (int.TryParse(genStr, out int gen))
                result.Add((gen, file));
        }

        // Sort descending by generation (newest first)
        result.Sort((a, b) => b.Item1.CompareTo(a.Item1));
        return result;
    }

    /// <summary>
    /// Tries to load and validate a specific commit file.
    /// Returns null if the file is corrupt or references missing segments.
    /// </summary>
    private static RecoveryResult? TryLoadCommit(string directoryPath, string commitFilePath, int generation)
    {
        try
        {
            var json = File.ReadAllText(commitFilePath);
            var commitData = JsonSerializer.Deserialize<CommitData>(json);
            if (commitData is null || commitData.Segments is null)
                return null;

            // Validate generation matches
            if (commitData.Generation != generation)
                return null;

            // Validate all referenced segments exist
            var validSegments = new List<string>();
            var missingSegments = new List<string>();
            foreach (var segId in commitData.Segments)
            {
                var segPath = Path.Combine(directoryPath, segId + ".seg");
                if (File.Exists(segPath))
                    validSegments.Add(segId);
                else
                    missingSegments.Add(segId);
            }

            // If any segments are missing, this commit is invalid
            if (missingSegments.Count > 0)
                return null;

            return new RecoveryResult
            {
                Generation = generation,
                ContentToken = commitData.ContentToken,
                SegmentIds = validSegments,
                CommitFilePath = commitFilePath,
                WasFallback = false
            };
        }
        catch (JsonException)
        {
            return null; // corrupt JSON
        }
        catch (IOException)
        {
            return null; // file read error
        }
    }

    /// <summary>
    /// Removes temp files left by interrupted write-then-rename commits.
    /// </summary>
    private static void CleanupTempFiles(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
            return;

        foreach (var tmpFile in Directory.GetFiles(directoryPath, "*.tmp"))
        {
            try { File.Delete(tmpFile); } catch { /* best-effort */ }
        }
    }

    /// <summary>
    /// Removes segment files that are not referenced by the active commit.
    /// </summary>
    private static void CleanupOrphanedSegments(string directoryPath, List<string> activeSegmentIds)
    {
        var activeSet = new HashSet<string>(activeSegmentIds, StringComparer.Ordinal);

        // Find all segment IDs on disk by looking for .seg files
        foreach (var segFile in Directory.GetFiles(directoryPath, "*.seg"))
        {
            var segId = Path.GetFileNameWithoutExtension(segFile);
            if (activeSet.Contains(segId))
                continue;

            // This segment is orphaned — remove all its files
            foreach (var ext in SegmentExtensions)
            {
                var orphanedFile = Path.Combine(directoryPath, segId + ext);
                try { if (File.Exists(orphanedFile)) File.Delete(orphanedFile); } catch { /* best-effort */ }
            }
        }
    }

    /// <summary>Result of crash recovery.</summary>
    public sealed class RecoveryResult
    {
        /// <summary>Gets the generation number of the recovered commit.</summary>
        public int Generation { get; init; }

        /// <summary>Gets the logical content token stored in the recovered commit.</summary>
        public long ContentToken { get; init; }

        /// <summary>Gets the segment IDs referenced by the recovered commit.</summary>
        public List<string> SegmentIds { get; init; } = [];

        /// <summary>Gets the file path of the commit file that was successfully loaded.</summary>
        public string CommitFilePath { get; init; } = "";

        /// <summary>Gets a value indicating whether recovery fell back to an older commit generation.</summary>
        public bool WasFallback { get; init; }
    }

    private sealed class CommitData
    {
        public List<string> Segments { get; set; } = [];
        public int Generation { get; set; }
        public long ContentToken { get; set; }
    }
}
