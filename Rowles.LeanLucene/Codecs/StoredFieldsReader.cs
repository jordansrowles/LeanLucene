using System.Buffers;
using System.IO.Compression;

namespace Rowles.LeanLucene.Codecs;

/// <summary>
/// Reads stored fields (.fdt) with Brotli block compression and multi-valued field support.
/// Paired with <see cref="StoredFieldsWriter"/>.
/// </summary>
public sealed class StoredFieldsReader : IDisposable
{
    private readonly FileStream _fs;
    private readonly BinaryReader _reader;
    private readonly int _blockSize;
    private readonly long[] _blockOffsets;

    // Decompressed block cache (last used block)
    private int _cachedBlockIndex = -1;
    private byte[]? _cachedBlockData;
    private int[]? _cachedIntraOffsets;

    // Reusable MemoryStream + BinaryReader for ReadDocument (avoid per-call allocation)
    private MemoryStream? _docStream;
    private BinaryReader? _docReader;

    private bool _disposed;

    private StoredFieldsReader(FileStream fs, BinaryReader reader, int blockSize, long[] blockOffsets)
    {
        _fs = fs;
        _reader = reader;
        _blockSize = blockSize;
        _blockOffsets = blockOffsets;
    }

    public static StoredFieldsReader Open(string fdtPath, string fdxPath)
    {
        using var fdxStream = new FileStream(fdxPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var fdxReader = new BinaryReader(fdxStream, System.Text.Encoding.UTF8, leaveOpen: false);

        _ = fdxReader.ReadByte(); // format marker
        int blockSize = fdxReader.ReadInt32();
        _ = fdxReader.ReadInt32(); // docCount
        int blockCount = fdxReader.ReadInt32();
        var blockOffsets = new long[blockCount];
        for (int i = 0; i < blockCount; i++)
            blockOffsets[i] = fdxReader.ReadInt64();

        var fs = new FileStream(fdtPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var reader = new BinaryReader(fs, System.Text.Encoding.UTF8, leaveOpen: true);
        return new StoredFieldsReader(fs, reader, blockSize, blockOffsets);
    }

    public Dictionary<string, List<string>> ReadDocument(int docId)
    {
        int blockIndex = docId / _blockSize;
        int docInBlock = docId % _blockSize;

        if (blockIndex != _cachedBlockIndex)
        {
            DecompressBlock(blockIndex);
            // Invalidate reusable stream since block data changed
            _docReader?.Dispose();
            _docStream?.Dispose();
            _docStream = new MemoryStream(_cachedBlockData!, 0, _cachedBlockData!.Length, writable: false, publiclyVisible: true);
            _docReader = new BinaryReader(_docStream, System.Text.Encoding.UTF8, leaveOpen: true);
        }
        else if (_docStream is null)
        {
            _docStream = new MemoryStream(_cachedBlockData!, 0, _cachedBlockData!.Length, writable: false, publiclyVisible: true);
            _docReader = new BinaryReader(_docStream, System.Text.Encoding.UTF8, leaveOpen: true);
        }

        _docStream!.Seek(_cachedIntraOffsets![docInBlock], SeekOrigin.Begin);
        var br = _docReader!;

        int fieldCount = br.ReadInt32();
        var fields = new Dictionary<string, List<string>>(fieldCount);

        for (int i = 0; i < fieldCount; i++)
        {
            int nameLen = br.ReadInt32();
            string name = System.Text.Encoding.UTF8.GetString(br.ReadBytes(nameLen));

            int valueCount = br.ReadInt32();
            var values = new List<string>(valueCount);
            for (int v = 0; v < valueCount; v++)
            {
                int valueLen = br.ReadInt32();
                string value = System.Text.Encoding.UTF8.GetString(br.ReadBytes(valueLen));
                values.Add(value);
            }
            fields[name] = values;
        }

        return fields;
    }

    private void DecompressBlock(int blockIndex)
    {
        _fs.Seek(_blockOffsets[blockIndex], SeekOrigin.Begin);

        int docCount = _reader.ReadInt32();
        int rawLength = _reader.ReadInt32();
        int compLength = _reader.ReadInt32();

        var intraOffsets = new int[docCount];
        for (int i = 0; i < docCount; i++)
            intraOffsets[i] = _reader.ReadInt32();

        var compData = ArrayPool<byte>.Shared.Rent(compLength);
        try
        {
            _reader.BaseStream.ReadExactly(compData.AsSpan(0, compLength));

            using var compStream = new MemoryStream(compData, 0, compLength);
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
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(compData);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _docReader?.Dispose();
        _docStream?.Dispose();
        _reader.Dispose();
        _fs.Dispose();
    }
}
