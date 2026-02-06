namespace Rowles.LeanLucene.Index.Segment;

/// <summary>
/// A bitset tracking which document IDs within a segment are parent documents.
/// Used by the block-join indexing pattern where child documents are stored
/// immediately before their parent document.
/// </summary>
public sealed class ParentBitSet
{
    private readonly long[] _bits;
    private readonly int _length;

    public ParentBitSet(int maxDoc)
    {
        _length = maxDoc;
        _bits = new long[(maxDoc + 63) >> 6];
    }

    private ParentBitSet(long[] bits, int length)
    {
        _bits = bits;
        _length = length;
    }

    /// <summary>Marks the given doc ID as a parent document.</summary>
    public void Set(int docId)
    {
        _bits[docId >> 6] |= 1L << (docId & 63);
    }

    /// <summary>Returns true if the given doc ID is a parent document.</summary>
    public bool IsParent(int docId)
    {
        if ((uint)docId >= (uint)_length) return false;
        return (_bits[docId >> 6] & (1L << (docId & 63))) != 0;
    }

    /// <summary>
    /// Returns the next parent doc ID at or after <paramref name="docId"/>,
    /// or -1 if no parent exists at or beyond that position.
    /// </summary>
    public int NextParent(int docId)
    {
        for (int i = docId; i < _length; i++)
        {
            if (IsParent(i)) return i;
        }
        return -1;
    }

    /// <summary>
    /// Returns the previous parent doc ID strictly before <paramref name="docId"/>,
    /// or -1 if no parent exists before that position.
    /// </summary>
    public int PrevParent(int docId)
    {
        for (int i = docId - 1; i >= 0; i--)
        {
            if (IsParent(i)) return i;
        }
        return -1;
    }

    public int Length => _length;

    /// <summary>Writes the bitset to a binary file (.pbs).</summary>
    public void WriteTo(string filePath)
    {
        using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
        using var bw = new BinaryWriter(fs);
        bw.Write(_length);
        bw.Write(_bits.Length);
        foreach (var word in _bits) bw.Write(word);
    }

    /// <summary>Reads a bitset from a binary file (.pbs).</summary>
    public static ParentBitSet ReadFrom(string filePath)
    {
        using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var br = new BinaryReader(fs);
        int length = br.ReadInt32();
        int wordCount = br.ReadInt32();
        var bits = new long[wordCount];
        for (int i = 0; i < wordCount; i++) bits[i] = br.ReadInt64();
        return new ParentBitSet(bits, length);
    }
}
