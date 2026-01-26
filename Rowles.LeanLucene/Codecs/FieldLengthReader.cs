using System.Text;
using Rowles.LeanLucene.Store;

namespace Rowles.LeanLucene.Codecs;

/// <summary>
/// Reads exact per-field per-doc token counts from a <c>.fln</c> file.
/// Returns <c>Dictionary&lt;string, int[]&gt;</c> keyed by field name.
/// Falls back gracefully when the file does not exist (caller should use quantised norms).
/// </summary>
public static class FieldLengthReader
{
    /// <summary>
    /// Tries to load exact field lengths. Returns null if the file does not exist.
    /// Throws on corrupt/invalid data.
    /// </summary>
    public static Dictionary<string, int[]>? TryRead(string filePath)
    {
        if (!File.Exists(filePath)) return null;

        using var input = new IndexInput(filePath);
        CodecConstants.ValidateHeader(input, CodecConstants.FieldLengthVersion, "field lengths (.fln)");

        int fieldCount = input.ReadInt32();
        var result = new Dictionary<string, int[]>(fieldCount, StringComparer.Ordinal);

        for (int f = 0; f < fieldCount; f++)
        {
            int nameLen = input.ReadInt32();
            var nameBytes = new byte[nameLen];
            for (int b = 0; b < nameLen; b++)
                nameBytes[b] = input.ReadByte();
            string fieldName = Encoding.UTF8.GetString(nameBytes);

            int docCount = input.ReadInt32();
            var lengths = new int[docCount];

            for (int d = 0; d < docCount; d++)
            {
                byte lo = input.ReadByte();
                byte hi = input.ReadByte();
                lengths[d] = lo | (hi << 8);
            }

            result[fieldName] = lengths;
        }

        return result;
    }
}
