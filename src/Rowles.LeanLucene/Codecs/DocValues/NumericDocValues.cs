using Rowles.LeanLucene.Store;

namespace Rowles.LeanLucene.Codecs.DocValues;

/// <summary>
/// Writes per-document numeric values in a compact column-stride format (.dvn).
/// Layout per field: [fieldName (length-prefixed UTF-8)] [docCount: int32] [minValue: int64]
/// [bitsPerValue: byte] [packed values...].
/// Uses delta-from-min encoding with bit packing.
/// </summary>
internal static class NumericDocValuesWriter
{
    public static void Write(string filePath, IReadOnlyDictionary<string, double[]> fields, int docCount, bool durable = false)
    {
        using var output = new IndexOutput(filePath, durable);

        CodecConstants.WriteHeader(output, CodecConstants.NumericDocValuesVersion);

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
            // Subtract in ulong space so mixed-sign bit patterns (e.g. negative and
            // positive doubles in the same column) do not overflow the long subtraction.
            // Two's-complement reinterpretation gives the correct unsigned distance.
            ulong range = (ulong)max - (ulong)min;
            int bitsPerValue = range == 0 ? 0 : 64 - System.Numerics.BitOperations.LeadingZeroCount(range);
            output.WriteByte((byte)bitsPerValue);

            if (bitsPerValue == 0) continue; // All values identical

            // Byte-level bitpacking (safe for any bitsPerValue 1-64)
            byte accum = 0;
            int accBits = 0;
            for (int i = 0; i < docCount; i++)
            {
                ulong delta = (ulong)BitConverter.DoubleToInt64Bits(values[i]) - (ulong)min;
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
