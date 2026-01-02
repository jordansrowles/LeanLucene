using System.IO.Compression;

namespace Rowles.LeanLucene.Codecs;

/// <summary>
/// Writes stored field data (.fdt) with Brotli block compression and a parallel offset index (.fdx).
/// Documents are grouped into blocks of 16 and compressed together.
/// </summary>
public static class StoredFieldsWriter
{
    private const int BlockSize = 16;

    public static void Write(string fdtPath, string fdxPath, IReadOnlyList<Dictionary<string, string>> docs)
    {
        using var fdtStream = new FileStream(fdtPath, FileMode.Create, FileAccess.Write, FileShare.None);
        using var fdtWriter = new BinaryWriter(fdtStream, System.Text.Encoding.UTF8, leaveOpen: false);

        // Header: version + block size
        fdtWriter.Write((byte)2); // version 2 = compressed
        fdtWriter.Write(BlockSize);

        var blockOffsets = new List<long>();

        // Reusable buffers across blocks
        var rawStream = new MemoryStream(4096);
        var rawWriter = new BinaryWriter(rawStream, System.Text.Encoding.UTF8, leaveOpen: true);
        var compStream = new MemoryStream(4096);

        for (int blockStart = 0; blockStart < docs.Count; blockStart += BlockSize)
        {
            int blockEnd = Math.Min(blockStart + BlockSize, docs.Count);
            int blockCount = blockEnd - blockStart;

            // Reset reusable streams
            rawStream.SetLength(0);
            rawStream.Position = 0;

            var intraOffsets = new int[blockCount];
            for (int i = 0; i < blockCount; i++)
            {
                intraOffsets[i] = (int)rawStream.Position;
                var fields = docs[blockStart + i];
                rawWriter.Write(fields.Count);
                foreach (var (name, value) in fields)
                {
                    var nameBytes = System.Text.Encoding.UTF8.GetBytes(name);
                    rawWriter.Write(nameBytes.Length);
                    rawWriter.Write(nameBytes);
                    var valueBytes = System.Text.Encoding.UTF8.GetBytes(value);
                    rawWriter.Write(valueBytes.Length);
                    rawWriter.Write(valueBytes);
                }
            }
            rawWriter.Flush();

            int rawLength = (int)rawStream.Length;

            // Compress with Brotli (quality 4 = fast), reuse compStream
            compStream.SetLength(0);
            compStream.Position = 0;
            using (var brotli = new BrotliStream(compStream, CompressionLevel.Fastest, leaveOpen: true))
            {
                brotli.Write(rawStream.GetBuffer().AsSpan(0, rawLength));
            }
            int compLength = (int)compStream.Length;

            // Write block: [int: docCount][int: rawLength][int: compLength][int[]: intraOffsets][byte[]: compData]
            blockOffsets.Add(fdtStream.Position);
            fdtWriter.Write(blockCount);
            fdtWriter.Write(rawLength);
            fdtWriter.Write(compLength);
            for (int i = 0; i < blockCount; i++)
                fdtWriter.Write(intraOffsets[i]);
            fdtWriter.Write(compStream.GetBuffer().AsSpan(0, compLength));
        }

        rawWriter.Dispose();
        rawStream.Dispose();
        compStream.Dispose();

        fdtWriter.Flush();

        // Write .fdx: [int: version][int: blockSize][int: blockCount][long[]: blockOffsets]
        using var fdxStream = new FileStream(fdxPath, FileMode.Create, FileAccess.Write, FileShare.None);
        using var fdxWriter = new BinaryWriter(fdxStream, System.Text.Encoding.UTF8, leaveOpen: false);

        fdxWriter.Write((byte)2); // version
        fdxWriter.Write(BlockSize);
        fdxWriter.Write(docs.Count);
        fdxWriter.Write(blockOffsets.Count);
        foreach (var offset in blockOffsets)
            fdxWriter.Write(offset);
    }
}
