using Rowles.LeanLucene.Store;

namespace Rowles.LeanLucene.Codecs.DocValues;

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
