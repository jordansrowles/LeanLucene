using Rowles.LeanLucene.Util;

namespace Rowles.LeanLucene.Index.Segment;

/// <summary>
/// Per-segment deletion tracker using a Roaring bitmap of deleted document IDs.
/// Sparse deletions use very little memory compared to the previous BitArray approach.
/// </summary>
internal sealed class LiveDocs
{
    private readonly RoaringBitmap _deletedDocs;
    private readonly int _maxDoc;

    public LiveDocs(int maxDoc)
    {
        _deletedDocs = new RoaringBitmap();
        _maxDoc = maxDoc;
    }

    private LiveDocs(RoaringBitmap deletedDocs, int maxDoc)
    {
        _deletedDocs = deletedDocs;
        _maxDoc = maxDoc;
    }

    public int LiveCount => _maxDoc - _deletedDocs.Cardinality;
    public int MaxDoc => _maxDoc;
    public int DeletedCount => _deletedDocs.Cardinality;

    public void Delete(int docId)
    {
        _deletedDocs.Add(docId);
    }

    public bool IsLive(int docId) => !_deletedDocs.Contains(docId);

    /// <summary>Returns the underlying deleted-docs bitmap for set operations.</summary>
    internal RoaringBitmap DeletedBitmap => _deletedDocs;

    public static void Serialise(string filePath, LiveDocs liveDocs)
    {
        liveDocs._deletedDocs.Serialise(filePath);
    }

    public static LiveDocs Deserialise(string filePath, int maxDoc)
    {
        var deletedDocs = RoaringBitmap.Deserialise(filePath);
        return new LiveDocs(deletedDocs, maxDoc);
    }
}
