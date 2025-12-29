using System.IO.Compression;

namespace Rowles.LeanLucene.Codecs;

/// <summary>
/// Writes stored field data (.fdt) with Brotli block compression and a parallel offset index (.fdx).
/// Documents are grouped into blocks of 16 and compressed together.
/// </summary>
public static class StoredFieldsWriter
{
    private const int BlockSize = 16;

    public static void Write(string fdtPath, string fdxPath, Dictionary<string, string>[] docs)
    {
        using var fdtStream = new FileStream(fdtPath, FileMode.Create, FileAccess.Write, FileShare.None);
        using var fdtWriter = new BinaryWriter(fdtStream, System.Text.Encoding.UTF8, leaveOpen: false);

        // Header: version + block size
        fdtWriter.Write((byte)2); // version 2 = compressed
        fdtWriter.Write(BlockSize);

        var blockOffsets = new List<long>();
        var docOffsetsInBlock = new List<int>(); // relative offsets within decompressed block

        for (int blockStart = 0; blockStart < docs.Length; blockStart += BlockSize)
        {
            int blockEnd = Math.Min(blockStart + BlockSize, docs.Length);
            int blockCount = blockEnd - blockStart;

            // Serialise the block's documents into a memory buffer
            using var rawStream = new MemoryStream();
            using var rawWriter = new BinaryWriter(rawStream, System.Text.Encoding.UTF8, leaveOpen: true);

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

            var rawData = rawStream.ToArray();

            // Compress with Brotli (quality 4 = fast)
            using var compStream = new MemoryStream();
            using (var brotli = new BrotliStream(compStream, CompressionLevel.Fastest, leaveOpen: true))
            {
                brotli.Write(rawData);
            }
            var compData = compStream.ToArray();

            // Write block: [int: docCount][int: rawLength][int: compLength][int[]: intraOffsets][byte[]: compData]
            blockOffsets.Add(fdtStream.Position);
            fdtWriter.Write(blockCount);
            fdtWriter.Write(rawData.Length);
            fdtWriter.Write(compData.Length);
            for (int i = 0; i < blockCount; i++)
                fdtWriter.Write(intraOffsets[i]);
            fdtWriter.Write(compData);
        }

        fdtWriter.Flush();

        // Write .fdx: [int: version][int: blockSize][int: blockCount][long[]: blockOffsets]
        using var fdxStream = new FileStream(fdxPath, FileMode.Create, FileAccess.Write, FileShare.None);
        using var fdxWriter = new BinaryWriter(fdxStream, System.Text.Encoding.UTF8, leaveOpen: false);

        fdxWriter.Write((byte)2); // version
        fdxWriter.Write(BlockSize);
        fdxWriter.Write(docs.Length);
        fdxWriter.Write(blockOffsets.Count);
        foreach (var offset in blockOffsets)
            fdxWriter.Write(offset);
    }
}
