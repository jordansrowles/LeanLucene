using Rowles.LeanLucene.Store;

namespace Rowles.LeanLucene.Codecs.DocValues;

/// <summary>
/// Writes per-document string values in a column-stride format (.dvs).
/// Layout: [fieldCount: int32] per field: [fieldName] [docCount: int32]
/// [ordCount: int32] [ord table: length-prefixed strings] [ords: packed ints].
/// Deduplicates values via an ordinal table.
/// </summary>
internal static class SortedDocValuesWriter
{
    public static void Write(string filePath, IReadOnlyDictionary<string, string?[]> fields, int docCount)
    {
        using var output = new IndexOutput(filePath);
        
        CodecConstants.WriteHeader(output, CodecConstants.SortedDocValuesVersion);
        
        output.WriteInt32(fields.Count);

        foreach (var (fieldName, values) in fields)
        {
            var nameBytes = System.Text.Encoding.UTF8.GetBytes(fieldName);
            output.WriteVarInt(nameBytes.Length);
            foreach (var b in nameBytes)
                output.WriteByte(b);

            output.WriteInt32(docCount);

            // Build ordinal table (sorted unique values)
            var ordMap = new Dictionary<string, int>(StringComparer.Ordinal);
            var ordList = new List<string>();
            for (int i = 0; i < docCount; i++)
            {
                var v = values[i] ?? string.Empty;
                if (!ordMap.ContainsKey(v))
                {
                    ordMap[v] = ordList.Count;
                    ordList.Add(v);
                }
            }

            // Sort ordinals lexicographically and remap
            ordList.Sort(StringComparer.Ordinal);
            for (int i = 0; i < ordList.Count; i++)
                ordMap[ordList[i]] = i;

            output.WriteInt32(ordList.Count);
            foreach (var ord in ordList)
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(ord);
                output.WriteVarInt(bytes.Length);
                foreach (var b in bytes)
                    output.WriteByte(b);
            }

            // Write ordinals per doc using packed ints
            int bitsPerOrd = ordList.Count <= 1 ? 0 : 64 - System.Numerics.BitOperations.LeadingZeroCount((ulong)(ordList.Count - 1));
            output.WriteByte((byte)bitsPerOrd);

            if (bitsPerOrd > 0)
            {
                ulong buffer = 0;
                int bitsInBuffer = 0;
                for (int i = 0; i < docCount; i++)
                {
                    int ord = ordMap[values[i] ?? string.Empty];
                    buffer |= (ulong)ord << bitsInBuffer;
                    bitsInBuffer += bitsPerOrd;
                    while (bitsInBuffer >= 8)
                    {
                        output.WriteByte((byte)(buffer & 0xFF));
                        buffer >>= 8;
                        bitsInBuffer -= 8;
                    }
                }
                if (bitsInBuffer > 0)
                    output.WriteByte((byte)(buffer & 0xFF));
            }
        }
    }
}

/// <summary>
/// Reads per-document string values from a column-stride .dvs file.
/// </summary>
internal static class SortedDocValuesReader
{
    public static Dictionary<string, string[]> Read(string filePath)
    {
        var result = new Dictionary<string, string[]>(StringComparer.Ordinal);
        if (!File.Exists(filePath)) return result;

        using var input = new IndexInput(filePath);
        
        CodecConstants.ValidateHeader(input, CodecConstants.SortedDocValuesVersion, "sorted doc values (.dvs)");
        
        int fieldCount = input.ReadInt32();

        for (int f = 0; f < fieldCount; f++)
        {
            int nameLen = input.ReadVarInt();
            var nameBytes = new byte[nameLen];
            for (int b = 0; b < nameLen; b++)
                nameBytes[b] = input.ReadByte();
            string fieldName = System.Text.Encoding.UTF8.GetString(nameBytes);

            int docCount = input.ReadInt32();
            int ordCount = input.ReadInt32();

            var ordTable = new string[ordCount];
            for (int o = 0; o < ordCount; o++)
            {
                int len = input.ReadVarInt();
                var bytes = new byte[len];
                for (int b = 0; b < len; b++)
                    bytes[b] = input.ReadByte();
                ordTable[o] = System.Text.Encoding.UTF8.GetString(bytes);
            }

            int bitsPerOrd = input.ReadByte();
            var values = new string[docCount];

            if (bitsPerOrd == 0)
            {
                Array.Fill(values, ordTable.Length > 0 ? ordTable[0] : string.Empty);
            }
            else
            {
                ulong mask = (1UL << bitsPerOrd) - 1;
                ulong buffer = 0;
                int bitsInBuffer = 0;
                for (int i = 0; i < docCount; i++)
                {
                    while (bitsInBuffer < bitsPerOrd)
                    {
                        buffer |= (ulong)input.ReadByte() << bitsInBuffer;
                        bitsInBuffer += 8;
                    }
                    int ord = (int)(buffer & mask);
                    buffer >>= bitsPerOrd;
                    bitsInBuffer -= bitsPerOrd;
                    values[i] = ordTable[ord];
                }
            }

            result[fieldName] = values;
        }

        return result;
    }
}
