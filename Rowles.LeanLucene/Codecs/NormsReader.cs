namespace Rowles.LeanLucene.Codecs;

/// <summary>
/// Reads quantised norm values and restores them to floats.
/// </summary>
public static class NormsReader
{
    public static float[] Read(string filePath)
    {
        var bytes = File.ReadAllBytes(filePath);
        var norms = new float[bytes.Length];

        for (int i = 0; i < bytes.Length; i++)
            norms[i] = bytes[i] / 255f;

        return norms;
    }
}
