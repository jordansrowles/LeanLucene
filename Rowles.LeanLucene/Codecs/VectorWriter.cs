namespace Rowles.LeanLucene.Codecs;

/// <summary>
/// Writes dense float vectors with a fixed-dimension layout for implicit offset indexing.
/// Format: [int: vectorCount][int: dimension][float[][]: vector data].
/// </summary>
public static class VectorWriter
{
    public static void Write(string filePath, float[][] vectors)
    {
        using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
        using var writer = new BinaryWriter(fs, System.Text.Encoding.UTF8, leaveOpen: false);

        int dimension = vectors.Length > 0 ? vectors[0].Length : 0;

        writer.Write(vectors.Length);
        writer.Write(dimension);

        for (int i = 0; i < vectors.Length; i++)
        {
            for (int j = 0; j < dimension; j++)
                writer.Write(vectors[i][j]);
        }
    }
}
