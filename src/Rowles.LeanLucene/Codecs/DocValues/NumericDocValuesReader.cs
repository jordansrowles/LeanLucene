using Rowles.LeanLucene.Store;

namespace Rowles.LeanLucene.Codecs.DocValues;

/// <summary>
/// Reads per-document numeric values from a column-stride .dvn file.
/// </summary>
internal static class NumericDocValuesReader
{
    public static Dictionary<string, double[]> Read(string filePath)
    {
        var result = new Dictionary<string, double[]>(StringComparer.Ordinal);
        if (!File.Exists(filePath)) return result;

        using var input = new IndexInput(filePath);
        
        CodecConstants.ValidateHeader(input, CodecConstants.NumericDocValuesVersion, "numeric doc values (.dvn)");
        
        int fieldCount = input.ReadInt32();

        for (int f = 0; f < fieldCount; f++)
        {
            int nameLen = input.ReadVarInt();
            var nameBytes = new byte[nameLen];
            for (int b = 0; b < nameLen; b++)
                nameBytes[b] = input.ReadByte();
            string fieldName = System.Text.Encoding.UTF8.GetString(nameBytes);

            int docCount = input.ReadInt32();
            long min = input.ReadInt64();
            int bitsPerValue = input.ReadByte();

            var values = new double[docCount];
            if (bitsPerValue == 0)
            {
                double constVal = BitConverter.Int64BitsToDouble(min);
                Array.Fill(values, constVal);
            }
            else
            {
                // Byte-level bitpacking reader (safe for any bitsPerValue 1-64)
                byte accum = 0;
                int accBits = 0;
                for (int i = 0; i < docCount; i++)
                {
                    ulong val = 0;
                    int collected = 0;
                    while (collected < bitsPerValue)
                    {
                        if (accBits == 0)
                        {
                            accum = input.ReadByte();
                            accBits = 8;
                        }
                        int take = Math.Min(bitsPerValue - collected, accBits);
                        val |= ((ulong)(accum & ((1 << take) - 1))) << collected;
                        accum >>= take;
                        accBits -= take;
                        collected += take;
                    }
                    values[i] = BitConverter.Int64BitsToDouble(min + (long)val);
                }
            }

            result[fieldName] = values;
        }

        return result;
    }
}
