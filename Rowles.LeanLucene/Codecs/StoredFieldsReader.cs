using System.IO.Compression;

namespace Rowles.LeanLucene.Codecs;

/// <summary>
/// Reads stored fields. Supports both legacy (v1) and Brotli-compressed (v2) formats.
/// </summary>
public sealed class StoredFieldsReader : IDisposable
{
    private readonly FileStream _fs;
    private readonly BinaryReader _reader;
    private readonly int _version;
    private readonly int _blockSize;
    private readonly int _docCount;
    private readonly long[] _blockOffsets;

    // Legacy v1 fields
    private readonly long[]? _legacyOffsets;

    // Decompressed block cache (last used block)
    private int _cachedBlockIndex = -1;
    private byte[]? _cachedBlockData;
    private int[]? _cachedIntraOffsets;
    private int _cachedBlockDocCount;

    private bool _disposed;

    private StoredFieldsReader(FileStream fs, BinaryReader reader, int version,
        int blockSize, int docCount, long[] blockOffsets, long[]? legacyOffsets)
    {
        _fs = fs;
        _reader = reader;
        _version = version;
        _blockSize = blockSize;
        _docCount = docCount;
        _blockOffsets = blockOffsets;
        _legacyOffsets = legacyOffsets;
    }

    public static StoredFieldsReader Open(string fdtPath, string fdxPath)
    {
        // Detect version by reading .fdx header
        using var fdxProbe = new FileStream(fdxPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        byte firstByte = (byte)fdxProbe.ReadByte();
        fdxProbe.Close();

        if (firstByte == 2) // v2 compressed
            return OpenV2(fdtPath, fdxPath);
        else
            return OpenV1(fdtPath, fdxPath);
    }

    private static StoredFieldsReader OpenV1(string fdtPath, string fdxPath)
    {
        using var fdxStream = new FileStream(fdxPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var fdxReader = new BinaryReader(fdxStream, System.Text.Encoding.UTF8, leaveOpen: false);

        int docCount = (int)(fdxStream.Length / sizeof(long));
        var offsets = new long[docCount];
        for (int i = 0; i < docCount; i++)
            offsets[i] = fdxReader.ReadInt64();

        var fs = new FileStream(fdtPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var reader = new BinaryReader(fs, System.Text.Encoding.UTF8, leaveOpen: true);
        return new StoredFieldsReader(fs, reader, 1, 1, docCount, [], offsets);
    }

    private static StoredFieldsReader OpenV2(string fdtPath, string fdxPath)
    {
        using var fdxStream = new FileStream(fdxPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var fdxReader = new BinaryReader(fdxStream, System.Text.Encoding.UTF8, leaveOpen: false);

        byte version = fdxReader.ReadByte();
        int blockSize = fdxReader.ReadInt32();
        int docCount = fdxReader.ReadInt32();
        int blockCount = fdxReader.ReadInt32();
        var blockOffsets = new long[blockCount];
        for (int i = 0; i < blockCount; i++)
            blockOffsets[i] = fdxReader.ReadInt64();

        var fs = new FileStream(fdtPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var reader = new BinaryReader(fs, System.Text.Encoding.UTF8, leaveOpen: true);
        return new StoredFieldsReader(fs, reader, version, blockSize, docCount, blockOffsets, null);
    }

    public Dictionary<string, string> ReadDocument(int docId)
    {
        if (_version == 1)
            return ReadDocumentV1(docId);
        return ReadDocumentV2(docId);
    }

    private Dictionary<string, string> ReadDocumentV1(int docId)
    {
        _fs.Seek(_legacyOffsets![docId], SeekOrigin.Begin);

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

    private Dictionary<string, string> ReadDocumentV2(int docId)
    {
        int blockIndex = docId / _blockSize;
        int docInBlock = docId % _blockSize;

        // Use cached block if available
        if (blockIndex != _cachedBlockIndex)
        {
            DecompressBlock(blockIndex);
        }

        // Read from decompressed block data
        using var ms = new MemoryStream(_cachedBlockData!);
        ms.Seek(_cachedIntraOffsets![docInBlock], SeekOrigin.Begin);
        using var br = new BinaryReader(ms, System.Text.Encoding.UTF8, leaveOpen: true);

        int fieldCount = br.ReadInt32();
        var fields = new Dictionary<string, string>(fieldCount);

        for (int i = 0; i < fieldCount; i++)
        {
            int nameLen = br.ReadInt32();
            string name = System.Text.Encoding.UTF8.GetString(br.ReadBytes(nameLen));
            int valueLen = br.ReadInt32();
            string value = System.Text.Encoding.UTF8.GetString(br.ReadBytes(valueLen));
            fields[name] = value;
        }

        return fields;
    }

    private void DecompressBlock(int blockIndex)
    {
        _fs.Seek(_blockOffsets[blockIndex], SeekOrigin.Begin);

        // Skip the .fdt header (version byte + blockSize int) on first block only
        if (blockIndex == 0)
        {
            _fs.Seek(_blockOffsets[0], SeekOrigin.Begin);
        }

        int docCount = _reader.ReadInt32();
        int rawLength = _reader.ReadInt32();
        int compLength = _reader.ReadInt32();

        var intraOffsets = new int[docCount];
        for (int i = 0; i < docCount; i++)
            intraOffsets[i] = _reader.ReadInt32();

        var compData = _reader.ReadBytes(compLength);

        // Decompress
        using var compStream = new MemoryStream(compData);
        using var brotli = new BrotliStream(compStream, CompressionMode.Decompress);
        var rawData = new byte[rawLength];
        int totalRead = 0;
        while (totalRead < rawLength)
        {
            int read = brotli.Read(rawData, totalRead, rawLength - totalRead);
            if (read == 0) break;
            totalRead += read;
        }

        _cachedBlockIndex = blockIndex;
        _cachedBlockData = rawData;
        _cachedIntraOffsets = intraOffsets;
        _cachedBlockDocCount = docCount;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _reader.Dispose();
        _fs.Dispose();
    }
}
