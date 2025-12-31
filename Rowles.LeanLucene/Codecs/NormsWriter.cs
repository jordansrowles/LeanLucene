using Rowles.LeanLucene.Store;

namespace Rowles.LeanLucene.Codecs;

/// <summary>
/// Quantises float norms to single bytes and writes them to disc.
/// </summary>
public static class NormsWriter
{
    public static void Write(string filePath, float[] norms)
    {
        using var output = new IndexOutput(filePath);

        for (int i = 0; i < norms.Length; i++)
        {
            byte quantised = (byte)Math.Clamp(MathF.Round(norms[i] * 255f), 0f, 255f);
            output.WriteByte(quantised);
        }
    }
}
