namespace Rowles.LeanLucene.Codecs;

/// <summary>
/// Quantises float norms to single bytes and writes them to disc.
/// </summary>
public static class NormsWriter
{
    public static void Write(string filePath, float[] norms)
    {
        using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
        using var writer = new BinaryWriter(fs, System.Text.Encoding.UTF8, leaveOpen: false);

        for (int i = 0; i < norms.Length; i++)
        {
            byte quantised = (byte)Math.Clamp(MathF.Round(norms[i] * 255f), 0f, 255f);
            writer.Write(quantised);
        }
    }
}
