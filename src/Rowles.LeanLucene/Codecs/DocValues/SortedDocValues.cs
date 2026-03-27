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
    public static void Write(string filePath, IReadOnlyDictionary<string, string?[]> fields, int docCount, bool durable = false)
    {
        using var output = new IndexOutput(filePath, durable);
        
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
