namespace Rowles.LeanLucene.Codecs;

/// <summary>
/// Reads stored fields using the .fdx offset index to seek into .fdt.
/// </summary>
public sealed class StoredFieldsReader : IDisposable
{
    private readonly FileStream _fs;
    private readonly BinaryReader _reader;
    private readonly long[] _offsets;
    private bool _disposed;

    private StoredFieldsReader(FileStream fs, BinaryReader reader, long[] offsets)
    {
        _fs = fs;
        _reader = reader;
        _offsets = offsets;
    }

    public static StoredFieldsReader Open(string fdtPath, string fdxPath)
    {
        using var fdxStream = new FileStream(fdxPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var fdxReader = new BinaryReader(fdxStream, System.Text.Encoding.UTF8, leaveOpen: false);

        int docCount = (int)(fdxStream.Length / sizeof(long));
        var offsets = new long[docCount];
        for (int i = 0; i < docCount; i++)
            offsets[i] = fdxReader.ReadInt64();

        var fs = new FileStream(fdtPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var reader = new BinaryReader(fs, System.Text.Encoding.UTF8, leaveOpen: true);
        return new StoredFieldsReader(fs, reader, offsets);
    }

    public Dictionary<string, string> ReadDocument(int docId)
    {
        _fs.Seek(_offsets[docId], SeekOrigin.Begin);

        int fieldCount = _reader.ReadInt32();
        var fields = new Dictionary<string, string>(fieldCount);

        for (int i = 0; i < fieldCount; i++)
        {
            int nameLen = _reader.ReadInt32();
            string name = new string(_reader.ReadChars(nameLen));
            int valueLen = _reader.ReadInt32();
            string value = new string(_reader.ReadChars(valueLen));
            fields[name] = value;
        }

        return fields;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _reader.Dispose();
        _fs.Dispose();
    }
}
