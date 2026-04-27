namespace Rowles.LeanLucene.Codecs.Vectors;

/// <summary>
/// Helpers for resolving per-field vector and HNSW file paths within a segment.
/// Convention: "<c>{segmentId}_v_{sanitisedFieldName}.{ext}</c>" so multiple vector fields
/// can co-exist in one segment without colliding with the legacy single-vector layout.
/// </summary>
internal static class VectorFilePaths
{
    /// <summary>Returns the per-field <c>.vec</c> file path.</summary>
    public static string VectorFile(string basePath, string fieldName)
        => $"{basePath}_v_{Sanitise(fieldName)}.vec";

    /// <summary>Returns the per-field <c>.hnsw</c> file path.</summary>
    public static string HnswFile(string basePath, string fieldName)
        => $"{basePath}_v_{Sanitise(fieldName)}.hnsw";

    /// <summary>
    /// Returns a filesystem-safe representation of <paramref name="fieldName"/>. Replaces any
    /// character that is not ASCII letter/digit with an underscore.
    /// </summary>
    public static string Sanitise(string fieldName)
    {
        ArgumentException.ThrowIfNullOrEmpty(fieldName);
        Span<char> buf = stackalloc char[fieldName.Length];
        for (int i = 0; i < fieldName.Length; i++)
        {
            char c = fieldName[i];
            buf[i] = (char.IsAsciiLetterOrDigit(c) || c == '_') ? c : '_';
        }
        return new string(buf);
    }
}
