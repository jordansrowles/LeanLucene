using Rowles.LeanLucene.Codecs.Postings;
using Rowles.LeanLucene.Store;

namespace Rowles.LeanLucene.Index.Segment;

/// <summary>
/// Postings-related methods for SegmentReader.
/// </summary>
public sealed partial class SegmentReader
{
    /// <summary>
    /// Returns a PostingsEnum cursor for the given qualified term (field\0term).
    /// Decodes the postings list once; caller must dispose.
    /// </summary>
    public PostingsEnum GetPostingsEnum(string qualifiedTerm)
    {
        if (!TryGetCachedOffset(qualifiedTerm, out long offset))
            return PostingsEnum.Empty;

        return PostingsEnum.Create(_posInput, offset);
    }

    /// <summary>
    /// Returns a PostingsEnum at a known postings offset, skipping the dictionary lookup.
    /// Use when the offset was already obtained from a term scan (e.g. prefix/wildcard).
    /// </summary>
    public PostingsEnum GetPostingsEnumAtOffset(long offset)
        => PostingsEnum.Create(_posInput, offset);

    /// <summary>
    /// Returns a PostingsEnum with decoded positions for phrase queries.
    /// </summary>
    public PostingsEnum GetPostingsEnumWithPositions(string qualifiedTerm)
    {
        if (!TryGetCachedOffset(qualifiedTerm, out long offset))
            return PostingsEnum.Empty;

        return PostingsEnum.CreateWithPositions(_posInput, offset, _postingsVersion);
    }

    /// <summary>Returns positional data for a term in a specific document, or null if unavailable.</summary>
    public int[]? GetPositions(string field, string term, int docId)
    {
        var qualifiedTerm = GetQualifiedTerm(field, term);
        if (!_dicReader.TryGetPostingsOffset(qualifiedTerm, out long offset))
            return null;

        return ReadPositionsAtOffset(offset, docId);
    }

    /// <summary>Returns positional data for a pre-built qualified term string.</summary>
    internal ReadOnlySpan<int> GetPositions(string qualifiedTerm, int docId)
    {
        if (!_dicReader.TryGetPostingsOffset(qualifiedTerm, out long offset))
            return ReadOnlySpan<int>.Empty;

        var positions = ReadPositionsAtOffset(offset, docId);
        return positions.AsSpan();
    }

    /// <summary>
    /// Returns the term frequency for a given term in a specific document.
    /// </summary>
    public int GetTermFrequency(string field, string term, int docId)
    {
        var qualifiedTerm = GetQualifiedTerm(field, term);
        if (!_dicReader.TryGetPostingsOffset(qualifiedTerm, out long offset))
            return 0;

        return ReadTermFrequency(offset, docId);
    }

    /// <summary>
    /// Returns the term frequency for a pre-built qualified term string.
    /// </summary>
    internal int GetTermFrequency(string qualifiedTerm, int docId)
    {
        if (!_dicReader.TryGetPostingsOffset(qualifiedTerm, out long offset))
            return 0;

        return ReadTermFrequency(offset, docId);
    }

    private int[] ReadPostingsAtOffset(long offset)
    {
        _posInput.Seek(offset);
        int count = _posInput.ReadInt32();
        // Skip past skip entries
        int skipCount = _posInput.ReadInt32();
        if (skipCount > 0)
            _posInput.Seek(_posInput.Position + skipCount * 8L);
        var ids = new int[count];
        int prev = 0;
        for (int i = 0; i < count; i++)
        {
            prev = ReadNextDocId(_posInput, prev);
            ids[i] = prev;
        }
        return ids;
    }

    private int ReadTermFrequency(long offset, int targetDocId)
    {
        _posInput.Seek(offset);
        int count = _posInput.ReadInt32();
        // Skip past skip entries
        int skipCount = _posInput.ReadInt32();
        if (skipCount > 0)
            _posInput.Seek(_posInput.Position + skipCount * 8L);

        var ids = new int[count];
        int prev = 0;
        for (int i = 0; i < count; i++)
        {
            prev = ReadNextDocId(_posInput, prev);
            ids[i] = prev;
        }

        bool hasFreqs = _posInput.ReadBoolean();
        if (!hasFreqs) return 1;

        for (int i = 0; i < count; i++)
        {
            int freq = _posInput.ReadVarInt();
            if (ids[i] == targetDocId)
                return freq;
        }

        return 0;
    }

    private int[]? ReadPositionsAtOffset(long offset, int docId)
    {
        _posInput.Seek(offset);
        int count = _posInput.ReadInt32();
        // Skip past skip entries
        int skipCount = _posInput.ReadInt32();
        if (skipCount > 0)
            _posInput.Seek(_posInput.Position + skipCount * 8L);

        // Stream through doc IDs to find target index (zero alloc)
        int targetIndex = -1;
        int prev = 0;
        for (int i = 0; i < count; i++)
        {
            prev = ReadNextDocId(_posInput, prev);
            if (prev == docId && targetIndex < 0)
                targetIndex = i;
        }
        if (targetIndex < 0) return null;

        bool hasFreqs = _posInput.ReadBoolean();
        if (hasFreqs)
        {
            for (int i = 0; i < count; i++)
                _posInput.ReadVarInt(); // skip freqs
        }

        bool hasPositions = _posInput.ReadBoolean();

        bool hasPayloads = false;
        if (_postingsVersion >= 2)
            hasPayloads = _posInput.ReadBoolean();

        if (!hasPositions) return null;

        for (int i = 0; i < targetIndex; i++)
        {
            int posCount = _posInput.ReadVarInt();
            for (int j = 0; j < posCount; j++)
            {
                _posInput.ReadVarInt(); // position delta
                if (hasPayloads)
                {
                    int payloadLen = _posInput.ReadVarInt();
                    if (payloadLen > 0)
                        _posInput.Seek(_posInput.Position + payloadLen);
                }
            }
        }

        int targetPosCount = _posInput.ReadVarInt();
        var positions = new int[targetPosCount];
        int prevPos = 0;
        for (int i = 0; i < targetPosCount; i++)
        {
            prevPos += _posInput.ReadVarInt();
            positions[i] = prevPos;
            if (hasPayloads)
            {
                int payloadLen = _posInput.ReadVarInt();
                if (payloadLen > 0)
                    _posInput.Seek(_posInput.Position + payloadLen);
            }
        }

        return positions;
    }

    private static int ReadNextDocId(IndexInput input, int previous)
    {
        int delta = input.ReadVarInt();
        if (delta < 0)
            throw new InvalidDataException("Postings data is corrupt: negative delta encountered.");

        try
        {
            return checked(previous + delta);
        }
        catch (OverflowException ex)
        {
            throw new InvalidDataException("Postings data is corrupt: doc ID delta overflow.", ex);
        }
    }
}
