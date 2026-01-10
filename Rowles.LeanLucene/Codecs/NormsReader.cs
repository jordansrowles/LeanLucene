using System.Text;

namespace Rowles.LeanLucene.Codecs;

/// <summary>
/// Reads quantised per-field norm values and restores them to floats.
/// </summary>
public static class NormsReader
{
    public static Dictionary<string, float[]> Read(string filePath)
    {
        var bytes = File.ReadAllBytes(filePath);
        if (bytes.Length == 0)
            return new Dictionary<string, float[]>(StringComparer.Ordinal);

        int offset = 0;
        int fieldCount = BitConverter.ToInt32(bytes, offset);
        offset += 4;

        var result = new Dictionary<string, float[]>(fieldCount, StringComparer.Ordinal);

        for (int f = 0; f < fieldCount; f++)
        {
            int fieldNameLen = BitConverter.ToInt32(bytes, offset);
            offset += 4;
            string fieldName = Encoding.UTF8.GetString(bytes, offset, fieldNameLen);
            offset += fieldNameLen;

            int docCount = BitConverter.ToInt32(bytes, offset);
            offset += 4;

            var norms = new float[docCount];
            for (int i = 0; i < docCount; i++)
                norms[i] = bytes[offset++] / 255f;

            result[fieldName] = norms;
        }

        return result;
    }
}
