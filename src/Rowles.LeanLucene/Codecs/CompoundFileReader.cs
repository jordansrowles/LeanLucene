namespace Rowles.LeanLucene.Codecs;

/// <summary>
/// Reads individual files from a compound file (.cfs) by seeking to the correct offset.
/// </summary>
internal sealed class CompoundFileReader : IDisposable
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

        CodecConstants.ValidateHeader(reader, CodecConstants.CompoundFileVersion, "compound file (.cfs)");

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
