namespace Rowles.LeanLucene.Index.Segment;

/// <summary>
/// Stored fields-related methods for SegmentReader.
/// </summary>
public sealed partial class SegmentReader
{
    public IReadOnlyDictionary<string, IReadOnlyList<string>> GetStoredFields(int docId)
    {
        if (_storedReader is null)
            return new Dictionary<string, IReadOnlyList<string>>();
        
        var raw = _storedReader.ReadDocument(docId);
        // Convert to read-only types
        return raw.ToDictionary(
            kvp => kvp.Key, 
            kvp => (IReadOnlyList<string>)kvp.Value.AsReadOnly());
    }
}
