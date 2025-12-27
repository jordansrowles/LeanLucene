namespace Rowles.LeanLucene.Codecs;

/// <summary>
/// Writes delta-encoded postings lists for a given term.
/// Deltas are encoded as variable-length integers (VarInt) for compactness.
/// Format: [int: termLength][chars: term][int: docIdCount][varint[]: deltaEncodedIds].
/// </summary>
public static class PostingsWriter
{
    public static void Write(string filePath, string term, int[] docIds)
    {
        using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
        using var writer = new BinaryWriter(fs, System.Text.Encoding.UTF8, leaveOpen: false);

        writer.Write(term.Length);
        writer.Write(term.ToCharArray());
        writer.Write(docIds.Length);

        int prev = 0;
        for (int i = 0; i < docIds.Length; i++)
        {
            WriteVarInt(writer, docIds[i] - prev);
            prev = docIds[i];
        }
    }

    /// <summary>Writes a non-negative integer using variable-length encoding (LEB128).</summary>
    private static void WriteVarInt(BinaryWriter writer, int value)
    {
        uint v = (uint)value;
        while (v >= 0x80)
        {
            writer.Write((byte)(v | 0x80));
            v >>= 7;
        }
        writer.Write((byte)v);
    }
}
