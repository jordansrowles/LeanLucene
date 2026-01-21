using System.IO.Compression;

namespace Rowles.LeanLucene.Codecs;

/// <summary>
/// Writes stored field data (.fdt) with Brotli block compression and a parallel offset index (.fdx).
/// Documents are grouped into blocks of 16 and compressed together.
/// Each field supports multiple values.
/// </summary>
public static class StoredFieldsWriter
{
    private const int DefaultBlockSize = 16;

    /// <summary>
    /// Write stored fields from a flat struct-of-arrays buffer (used by IndexWriter flush path).
    /// </summary>
    internal static void Write(string fdtPath, string fdxPath,
        List<int> docStarts, List<int> fieldIds, List<string> values, List<string> fieldNames,
        int blockSize = DefaultBlockSize, CompressionLevel compressionLevel = CompressionLevel.Fastest)
    {
        int docCount = docStarts.Count;

        using var fdtStream = new FileStream(fdtPath, FileMode.Create, FileAccess.Write, FileShare.None);
        using var fdtWriter = new BinaryWriter(fdtStream, System.Text.Encoding.UTF8, leaveOpen: false);

        CodecConstants.WriteHeader(fdtWriter, CodecConstants.StoredFieldsVersion);
        fdtWriter.Write(blockSize);

        var blockOffsets = new List<long>();

        var rawStream = new MemoryStream(4096);
        var rawWriter = new BinaryWriter(rawStream, System.Text.Encoding.UTF8, leaveOpen: true);
        var compStream = new MemoryStream(4096);
        Span<byte> encodeBuf = stackalloc byte[512];

        // Temp buffers for per-doc field grouping
        var distinctFieldIds = new List<int>(16);

        for (int blockStart = 0; blockStart < docCount; blockStart += blockSize)
        {
            int blockEnd = Math.Min(blockStart + blockSize, docCount);
            int blockDocCount = blockEnd - blockStart;

            rawStream.SetLength(0);
            rawStream.Position = 0;

            var intraOffsets = new int[blockDocCount];
            for (int d = 0; d < blockDocCount; d++)
            {
                intraOffsets[d] = (int)rawStream.Position;
                int docIdx = blockStart + d;
                int entryStart = docStarts[docIdx];
                int entryEnd = docIdx + 1 < docCount ? docStarts[docIdx + 1] : fieldIds.Count;

                // Find distinct fields for this doc
                distinctFieldIds.Clear();
                for (int e = entryStart; e < entryEnd; e++)
                {
                    int fid = fieldIds[e];
                    if (!distinctFieldIds.Contains(fid))
                        distinctFieldIds.Add(fid);
                }

                rawWriter.Write(distinctFieldIds.Count);
                foreach (int fid in distinctFieldIds)
                {
                    string name = fieldNames[fid];
                    int nameByteCount = System.Text.Encoding.UTF8.GetByteCount(name);
                    Span<byte> nameBuf = nameByteCount <= encodeBuf.Length ? encodeBuf : new byte[nameByteCount];
                    System.Text.Encoding.UTF8.GetBytes(name, nameBuf);
                    rawWriter.Write(nameByteCount);
                    rawWriter.Write(nameBuf[..nameByteCount]);

                    // Count values for this field
                    int valueCount = 0;
                    for (int e = entryStart; e < entryEnd; e++)
                        if (fieldIds[e] == fid) valueCount++;
                    rawWriter.Write(valueCount);

                    for (int e = entryStart; e < entryEnd; e++)
                    {
                        if (fieldIds[e] != fid) continue;
                        string value = values[e];
                        int valueByteCount = System.Text.Encoding.UTF8.GetByteCount(value);
                        Span<byte> valueBuf = valueByteCount <= encodeBuf.Length ? encodeBuf : new byte[valueByteCount];
                        System.Text.Encoding.UTF8.GetBytes(value, valueBuf);
                        rawWriter.Write(valueByteCount);
                        rawWriter.Write(valueBuf[..valueByteCount]);
                    }
                }
            }
            rawWriter.Flush();

            int rawLength = (int)rawStream.Length;
            compStream.SetLength(0);
            compStream.Position = 0;
            using (var brotli = new BrotliStream(compStream, compressionLevel, leaveOpen: true))
            {
                brotli.Write(rawStream.GetBuffer().AsSpan(0, rawLength));
            }
            int compLength = (int)compStream.Length;

            blockOffsets.Add(fdtStream.Position);
            fdtWriter.Write(blockDocCount);
            fdtWriter.Write(rawLength);
            fdtWriter.Write(compLength);
            for (int i = 0; i < blockDocCount; i++)
                fdtWriter.Write(intraOffsets[i]);
            fdtWriter.Write(compStream.GetBuffer().AsSpan(0, compLength));
        }

        rawWriter.Dispose();
        rawStream.Dispose();
        compStream.Dispose();

        fdtWriter.Flush();

        using var fdxStream = new FileStream(fdxPath, FileMode.Create, FileAccess.Write, FileShare.None);
        using var fdxWriter = new BinaryWriter(fdxStream, System.Text.Encoding.UTF8, leaveOpen: false);

        CodecConstants.WriteHeader(fdxWriter, CodecConstants.StoredFieldsVersion);
        fdxWriter.Write(blockSize);
        fdxWriter.Write(docCount);
        fdxWriter.Write(blockOffsets.Count);
        foreach (var offset in blockOffsets)
            fdxWriter.Write(offset);
    }

    internal static void Write(string fdtPath, string fdxPath, IReadOnlyList<Dictionary<string, List<string>>> docs,
        int blockSize = DefaultBlockSize, CompressionLevel compressionLevel = CompressionLevel.Fastest)
    {
        using var fdtStream = new FileStream(fdtPath, FileMode.Create, FileAccess.Write, FileShare.None);
        using var fdtWriter = new BinaryWriter(fdtStream, System.Text.Encoding.UTF8, leaveOpen: false);

        // Header: codec header + block size
        CodecConstants.WriteHeader(fdtWriter, CodecConstants.StoredFieldsVersion);
        fdtWriter.Write(blockSize);

        var blockOffsets = new List<long>();

        // Reusable buffers across blocks
        var rawStream = new MemoryStream(4096);
        var rawWriter = new BinaryWriter(rawStream, System.Text.Encoding.UTF8, leaveOpen: true);
        var compStream = new MemoryStream(4096);
        Span<byte> encodeBuf = stackalloc byte[512];

        for (int blockStart = 0; blockStart < docs.Count; blockStart += blockSize)
        {
            int blockEnd = Math.Min(blockStart + blockSize, docs.Count);
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
                foreach (var (name, values) in fields)
                {
                    int nameByteCount = System.Text.Encoding.UTF8.GetByteCount(name);
                    Span<byte> nameBuf = nameByteCount <= encodeBuf.Length ? encodeBuf : new byte[nameByteCount];
                    System.Text.Encoding.UTF8.GetBytes(name, nameBuf);
                    rawWriter.Write(nameByteCount);
                    rawWriter.Write(nameBuf[..nameByteCount]);

                    // Write value count, then each value
                    rawWriter.Write(values.Count);
                    foreach (var value in values)
                    {
                        int valueByteCount = System.Text.Encoding.UTF8.GetByteCount(value);
                        Span<byte> valueBuf = valueByteCount <= encodeBuf.Length ? encodeBuf : new byte[valueByteCount];
                        System.Text.Encoding.UTF8.GetBytes(value, valueBuf);
                        rawWriter.Write(valueByteCount);
                        rawWriter.Write(valueBuf[..valueByteCount]);
                    }
                }
            }
            rawWriter.Flush();

            int rawLength = (int)rawStream.Length;

            // Compress with Brotli (quality 4 = fast), reuse compStream
            compStream.SetLength(0);
            compStream.Position = 0;
            using (var brotli = new BrotliStream(compStream, compressionLevel, leaveOpen: true))
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

        // Write .fdx: [header][int: blockSize][int: docCount][int: blockCount][long[]: blockOffsets]
        using var fdxStream = new FileStream(fdxPath, FileMode.Create, FileAccess.Write, FileShare.None);
        using var fdxWriter = new BinaryWriter(fdxStream, System.Text.Encoding.UTF8, leaveOpen: false);

        CodecConstants.WriteHeader(fdxWriter, CodecConstants.StoredFieldsVersion);
        fdxWriter.Write(blockSize);
        fdxWriter.Write(docs.Count);
        fdxWriter.Write(blockOffsets.Count);
        foreach (var offset in blockOffsets)
            fdxWriter.Write(offset);
    }
}
