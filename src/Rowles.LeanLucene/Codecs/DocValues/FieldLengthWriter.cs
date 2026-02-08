using System.Text;
using Rowles.LeanLucene.Store;

namespace Rowles.LeanLucene.Codecs.DocValues;

/// <summary>
/// Writes exact per-field per-doc token counts to a <c>.fln</c> file.
/// Layout: [Header][FieldCount:int32]([FieldNameLen:int32][FieldNameUTF8][DocCount:int32][ushort * DocCount])*
/// </summary>
internal static class FieldLengthWriter
{
    internal static void Write(string filePath, IReadOnlyDictionary<string, int[]> fieldTokenCounts, int docCount = -1)
    {
        using var output = new IndexOutput(filePath);

        CodecConstants.WriteHeader(output, CodecConstants.FieldLengthVersion);
        output.WriteInt32(fieldTokenCounts.Count);

        foreach (var (fieldName, counts) in fieldTokenCounts)
        {
            int count = docCount >= 0 ? docCount : counts.Length;
            var fieldBytes = Encoding.UTF8.GetBytes(fieldName);
            output.WriteInt32(fieldBytes.Length);
            output.WriteBytes(fieldBytes);
            output.WriteInt32(count);

            for (int i = 0; i < count; i++)
            {
                ushort val = (ushort)Math.Clamp(counts[i], 0, ushort.MaxValue);
                output.WriteByte((byte)(val & 0xFF));
                output.WriteByte((byte)(val >> 8));
            }
        }
    }
}
