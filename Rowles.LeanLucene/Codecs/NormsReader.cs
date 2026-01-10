using System.Text;

namespace Rowles.LeanLucene.Codecs;

/// <summary>
/// Reads quantised norm values and restores them to floats.
/// Supports both legacy (version 1, combined) and per-field (version 2) formats.
/// </summary>
public static class NormsReader
{
    /// <summary>
    /// Reads per-field norms from versioned format.
    /// Returns a dictionary with field names as keys.
    /// For legacy format (version 1 or no version), returns single entry with key "_combined".
    /// </summary>
    public static Dictionary<string, float[]> Read(string filePath)
    {
        var bytes = File.ReadAllBytes(filePath);
        if (bytes.Length == 0)
            return new Dictionary<string, float[]>(StringComparer.Ordinal);

        int offset = 0;
        
        // Check for version byte (version 1 or 2)
        // Legacy files (pre-versioning) have no version byte, start directly with norm bytes
        byte version = bytes[0];
        
        if (version == 2)
        {
            // Version 2: per-field format
            offset = 1;
            int fieldCount = BitConverter.ToInt32(bytes, offset);
            offset += 4;
            
            var result = new Dictionary<string, float[]>(fieldCount, StringComparer.Ordinal);
            
            for (int f = 0; f < fieldCount; f++)
            {
                // Read field name
                int fieldNameLen = BitConverter.ToInt32(bytes, offset);
                offset += 4;
                string fieldName = Encoding.UTF8.GetString(bytes, offset, fieldNameLen);
                offset += fieldNameLen;
                
                // Read document count
                int docCount = BitConverter.ToInt32(bytes, offset);
                offset += 4;
                
                // Read norms
                var norms = new float[docCount];
                for (int i = 0; i < docCount; i++)
                {
                    norms[i] = bytes[offset++] / 255f;
                }
                
                result[fieldName] = norms;
            }
            
            return result;
        }
        else if (version == 1)
        {
            // Version 1: legacy combined format with version byte
            offset = 1;
            var norms = new float[bytes.Length - 1];
            for (int i = 0; i < norms.Length; i++)
                norms[i] = bytes[offset++] / 255f;
            
            return new Dictionary<string, float[]>(StringComparer.Ordinal)
            {
                ["_combined"] = norms
            };
        }
        else
        {
            // Pre-versioning: no version byte, all bytes are norms (combined)
            var norms = new float[bytes.Length];
            for (int i = 0; i < bytes.Length; i++)
                norms[i] = bytes[i] / 255f;
            
            return new Dictionary<string, float[]>(StringComparer.Ordinal)
            {
                ["_combined"] = norms
            };
        }
    }
}
