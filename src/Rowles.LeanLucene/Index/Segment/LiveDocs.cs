using System.Collections;
using Rowles.LeanLucene.Store;

namespace Rowles.LeanLucene.Index.Segment;

/// <summary>
/// Per-segment bitset tracking deleted document IDs.
/// </summary>
internal sealed class LiveDocs
{
    private readonly BitArray _bits;
    private int _liveCount;

    public LiveDocs(int maxDoc)
    {
        // All documents are initially live (true).
        _bits = new BitArray(maxDoc, true);
        _liveCount = maxDoc;
    }

    private LiveDocs(BitArray bits)
    {
        _bits = bits;
        _liveCount = 0;
        for (int i = 0; i < bits.Length; i++)
        {
            if (bits[i]) _liveCount++;
        }
    }

    public int LiveCount => _liveCount;
    public int MaxDoc => _bits.Length;

    public void Delete(int docId)
    {
        if (_bits[docId])
        {
            _bits[docId] = false;
            _liveCount--;
        }
    }

    public bool IsLive(int docId) => _bits[docId];

    public static void Serialise(string filePath, LiveDocs liveDocs)
    {
        // Write as raw bytes: [int: length] [bytes: packed bits]
        var bytes = new byte[(liveDocs._bits.Length + 7) / 8];
        liveDocs._bits.CopyTo(bytes, 0);

        using var writer = new BinaryWriter(File.Create(filePath));
        writer.Write(liveDocs._bits.Length);
        writer.Write(bytes);
    }

    public static LiveDocs Deserialise(string filePath, int maxDoc)
    {
        using var input = new IndexInput(filePath);
        var length = input.ReadInt32();
        var bytes = input.ReadBytes((length + 7) / 8);
        var bits = new BitArray(bytes) { Length = length };
        return new LiveDocs(bits);
    }
}
