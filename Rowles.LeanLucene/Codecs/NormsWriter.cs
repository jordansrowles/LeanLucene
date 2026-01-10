using System.Text;
using Rowles.LeanLucene.Store;

namespace Rowles.LeanLucene.Codecs;

/// <summary>
/// Quantises float norms to single bytes and writes them to disc.
/// Writes per-field norms for accurate BM25 field-length normalisation.
/// </summary>
public static class NormsWriter
{
    public static void Write(string filePath, IReadOnlyDictionary<string, float[]> fieldNorms)
    {
        using var output = new IndexOutput(filePath);
        
        output.WriteInt32(fieldNorms.Count);
        
        foreach (var (fieldName, norms) in fieldNorms)
        {
            var fieldBytes = Encoding.UTF8.GetBytes(fieldName);
            output.WriteInt32(fieldBytes.Length);
            output.WriteBytes(fieldBytes);
            
            output.WriteInt32(norms.Length);
            
            for (int i = 0; i < norms.Length; i++)
            {
                byte quantised = (byte)Math.Clamp(MathF.Round(norms[i] * 255f), 0f, 255f);
                output.WriteByte(quantised);
            }
        }
    }
}
