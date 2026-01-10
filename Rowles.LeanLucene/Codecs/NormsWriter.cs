using System.Text;
using Rowles.LeanLucene.Store;

namespace Rowles.LeanLucene.Codecs;

/// <summary>
/// Quantises float norms to single bytes and writes them to disc.
/// Supports per-field norms for accurate BM25 scoring.
/// </summary>
public static class NormsWriter
{
    /// <summary>
    /// Writes per-field norms in versioned format (version 2).
    /// </summary>
    public static void Write(string filePath, IReadOnlyDictionary<string, float[]> fieldNorms)
    {
        using var output = new IndexOutput(filePath);
        
        // Version byte = 2 (per-field format)
        output.WriteByte(2);
        
        // Field count
        output.WriteInt32(fieldNorms.Count);
        
        foreach (var (fieldName, norms) in fieldNorms)
        {
            // Field name length and UTF-8 bytes
            var fieldBytes = Encoding.UTF8.GetBytes(fieldName);
            output.WriteInt32(fieldBytes.Length);
            output.WriteBytes(fieldBytes);
            
            // Document count for this field
            output.WriteInt32(norms.Length);
            
            // Quantised norms
            for (int i = 0; i < norms.Length; i++)
            {
                byte quantised = (byte)Math.Clamp(MathF.Round(norms[i] * 255f), 0f, 255f);
                output.WriteByte(quantised);
            }
        }
    }

    /// <summary>
    /// Writes combined norms in legacy format (version 1) for backward compatibility.
    /// </summary>
    [Obsolete("Use per-field Write overload for accurate BM25 scoring")]
    public static void Write(string filePath, float[] norms)
    {
        using var output = new IndexOutput(filePath);
        
        // Version byte = 1 (legacy combined format)
        output.WriteByte(1);

        for (int i = 0; i < norms.Length; i++)
        {
            byte quantised = (byte)Math.Clamp(MathF.Round(norms[i] * 255f), 0f, 255f);
            output.WriteByte(quantised);
        }
    }
}
