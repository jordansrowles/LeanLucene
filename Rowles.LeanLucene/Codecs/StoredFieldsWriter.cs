namespace Rowles.LeanLucene.Codecs;

/// <summary>
/// Writes stored field data (.fdt) and a parallel offset index (.fdx).
/// </summary>
public static class StoredFieldsWriter
{
    public static void Write(string fdtPath, string fdxPath, Dictionary<string, string>[] docs)
    {
        using var fdtStream = new FileStream(fdtPath, FileMode.Create, FileAccess.Write, FileShare.None);
        using var fdtWriter = new BinaryWriter(fdtStream, System.Text.Encoding.UTF8, leaveOpen: false);

        var offsets = new long[docs.Length];

        for (int i = 0; i < docs.Length; i++)
        {
            offsets[i] = fdtStream.Position;
            var fields = docs[i];

            fdtWriter.Write(fields.Count);
            foreach (var (name, value) in fields)
            {
                fdtWriter.Write(name.Length);
                fdtWriter.Write(name.ToCharArray());
                fdtWriter.Write(value.Length);
                fdtWriter.Write(value.ToCharArray());
            }
        }

        fdtWriter.Flush();

        // Write the offset index.
        using var fdxStream = new FileStream(fdxPath, FileMode.Create, FileAccess.Write, FileShare.None);
        using var fdxWriter = new BinaryWriter(fdxStream, System.Text.Encoding.UTF8, leaveOpen: false);

        for (int i = 0; i < offsets.Length; i++)
            fdxWriter.Write(offsets[i]);
    }
}
