using Rowles.LeanLucene.Store;

namespace Rowles.LeanLucene.Codecs;

/// <summary>
/// Writes per-document numeric values in a compact column-stride format (.dvn).
/// Layout per field: [fieldName (length-prefixed UTF-8)] [docCount: int32] [minValue: int64]
/// [bitsPerValue: byte] [packed values...].
/// Uses delta-from-min encoding with bit packing.
/// </summary>
internal static class NumericDocValuesWriter
{
    public static void Write(string filePath, IReadOnlyDictionary<string, double[]> fields, int docCount)
    {
        using var output = new IndexOutput(filePath);
        output.WriteInt32(fields.Count);

        foreach (var (fieldName, values) in fields)
        {
            // Write field name as length-prefixed UTF-8
            var nameBytes = System.Text.Encoding.UTF8.GetBytes(fieldName);
            output.WriteVarInt(nameBytes.Length);
            foreach (var b in nameBytes)
                output.WriteByte(b);

            output.WriteInt32(docCount);

            // Find min and max to compute delta range
            long min = long.MaxValue, max = long.MinValue;
            for (int i = 0; i < docCount; i++)
            {
                long v = BitConverter.DoubleToInt64Bits(values[i]);
                if (v < min) min = v;
                if (v > max) max = v;
            }

            output.WriteInt64(min);
            ulong range = (ulong)(max - min);
            int bitsPerValue = range == 0 ? 0 : 64 - System.Numerics.BitOperations.LeadingZeroCount(range);
            output.WriteByte((byte)bitsPerValue);

            if (bitsPerValue == 0) continue; // All values identical

            // Byte-level bitpacking (safe for any bitsPerValue 1-64)
            byte accum = 0;
            int accBits = 0;
            for (int i = 0; i < docCount; i++)
            {
                ulong delta = (ulong)(BitConverter.DoubleToInt64Bits(values[i]) - min);
                int remaining = bitsPerValue;
                while (remaining > 0)
                {
                    int space = 8 - accBits;
                    int take = Math.Min(remaining, space);
                    accum |= (byte)((delta & ((1UL << take) - 1)) << accBits);
                    delta >>= take;
                    accBits += take;
                    remaining -= take;
                    if (accBits == 8)
                    {
                        output.WriteByte(accum);
                        accum = 0;
                        accBits = 0;
                    }
                }
            }
            if (accBits > 0)
                output.WriteByte(accum);
        }
    }
}

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
