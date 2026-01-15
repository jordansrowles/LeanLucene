namespace Rowles.LeanLucene.Codecs;

/// <summary>
/// Combines multiple segment files into a single compound file (.cfs) to reduce file descriptor pressure.
/// Format: [entryCount:int32] per entry: [name:string] [offset:int64] [length:int64] [concatenated data...]
/// </summary>
public static class CompoundFileWriter
{
    internal static void Write(string cfsPath, string basePath, string[] extensions)
    {
        using var cfsFs = new FileStream(cfsPath, FileMode.Create, FileAccess.Write, FileShare.None);
        using var writer = new BinaryWriter(cfsFs, System.Text.Encoding.UTF8, leaveOpen: false);

        // Collect files that exist
        var entries = new List<(string Name, string FullPath)>();
        foreach (var ext in extensions)
        {
            var fullPath = basePath + ext;
            if (File.Exists(fullPath))
                entries.Add((ext, fullPath));
        }

        writer.Write(entries.Count);
        // Reserve space for the directory (name + offset + length per entry)
        long directoryStart = cfsFs.Position;
        foreach (var (name, _) in entries)
        {
            writer.Write(name);
            writer.Write(0L); // placeholder offset
            writer.Write(0L); // placeholder length
        }

        // Write file data and record offsets using chunked stream copy
        var offsets = new (long Offset, long Length)[entries.Count];
        var copyBuf = new byte[65536];
        for (int i = 0; i < entries.Count; i++)
        {
            offsets[i].Offset = cfsFs.Position;
            using var srcFs = new FileStream(entries[i].FullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            long totalCopied = 0;
            int bytesRead;
            while ((bytesRead = srcFs.Read(copyBuf, 0, copyBuf.Length)) > 0)
            {
                cfsFs.Write(copyBuf, 0, bytesRead);
                totalCopied += bytesRead;
            }
            offsets[i].Length = totalCopied;
        }

        // Go back and fill in the directory
        cfsFs.Seek(directoryStart, SeekOrigin.Begin);
        for (int i = 0; i < entries.Count; i++)
        {
            writer.Write(entries[i].Name);
            writer.Write(offsets[i].Offset);
            writer.Write(offsets[i].Length);
        }
    }
}

/// <summary>
/// Reads individual files from a compound file (.cfs) by seeking to the correct offset.
/// </summary>
public sealed class CompoundFileReader : IDisposable
{
    private readonly FileStream _fs;
    private readonly Dictionary<string, (long Offset, long Length)> _entries;

    private CompoundFileReader(FileStream fs, Dictionary<string, (long, long)> entries)
    {
        _fs = fs;
        _entries = entries;
    }

    public static CompoundFileReader Open(string cfsPath)
    {
        var fs = new FileStream(cfsPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var reader = new BinaryReader(fs, System.Text.Encoding.UTF8, leaveOpen: true);

        int entryCount = reader.ReadInt32();
        var entries = new Dictionary<string, (long, long)>(entryCount, StringComparer.Ordinal);
        for (int i = 0; i < entryCount; i++)
        {
            string name = reader.ReadString();
            long offset = reader.ReadInt64();
            long length = reader.ReadInt64();
            entries[name] = (offset, length);
        }

        reader.Dispose();
        return new CompoundFileReader(fs, entries);
    }

    /// <summary>Reads the complete data for a sub-file within the compound file.</summary>
    public byte[] ReadFile(string extension)
    {
        if (!_entries.TryGetValue(extension, out var entry))
            throw new FileNotFoundException($"Extension '{extension}' not found in compound file.");

        var buffer = new byte[entry.Length];
        _fs.Seek(entry.Offset, SeekOrigin.Begin);
        _fs.ReadExactly(buffer);
        return buffer;
    }

    public bool HasFile(string extension) => _entries.ContainsKey(extension);

    public IEnumerable<string> ListFiles() => _entries.Keys;

    public void Dispose() => _fs.Dispose();
}
