using System.Buffers;
using Rowles.LeanLucene.Store;

namespace Rowles.LeanLucene.Codecs;

/// <summary>
/// Forward-only cursor over a postings list. Decodes doc IDs and frequencies
/// once into ArrayPool-rented buffers, then yields (DocId, Freq) pairs via MoveNext().
/// </summary>
public struct PostingsEnum : IDisposable
{
    private int[]? _docIds;
    private int[]? _freqs;
    private readonly int _count;
    private int _index;
    private bool _disposed;

    public int DocFreq => _count;
    public int DocId => _index >= 0 && _index < _count ? _docIds![_index] : -1;
    public int Freq => _index >= 0 && _index < _count && _freqs is not null ? _freqs[_index] : 1;
    public bool IsExhausted => _count == 0;

    private PostingsEnum(int[]? docIds, int[]? freqs, int count)
    {
        _docIds = docIds;
        _freqs = freqs;
        _count = count;
        _index = -1;
        _disposed = false;
    }

    /// <summary>Creates a PostingsEnum by reading from a memory-mapped IndexInput at the specified offset.</summary>
    public static PostingsEnum Create(IndexInput input, long offset)
    {
        input.Seek(offset);
        int count = input.ReadInt32();
        if (count <= 0)
            return Empty;

        // Skip past skip pointer entries
        int skipCount = input.ReadInt32();
        if (skipCount > 0)
            input.Seek(input.Position + skipCount * 8L);

        var docIds = ArrayPool<int>.Shared.Rent(count);
        int prev = 0;
        for (int i = 0; i < count; i++)
        {
            prev += input.ReadVarInt();
            docIds[i] = prev;
        }

        bool hasFreqs = input.ReadBoolean();
        int[]? freqs = null;
        if (hasFreqs)
        {
            freqs = ArrayPool<int>.Shared.Rent(count);
            for (int i = 0; i < count; i++)
                freqs[i] = input.ReadVarInt();
        }

        return new PostingsEnum(docIds, freqs, count);
    }

    public bool MoveNext()
    {
        if (++_index < _count)
            return true;
        _index = _count;
        return false;
    }

    public void Reset() => _index = -1;

    /// <summary>
    /// Advances to the first document >= targetDocId. Returns true if found.
    /// Uses binary search on the pre-decoded docId array for O(log n) skip.
    /// </summary>
    public bool Advance(int targetDocId)
    {
        if (_docIds is null || _count == 0) return false;

        int startIndex = Math.Max(0, _index);
        int lo = startIndex, hi = _count - 1;
        int best = _count; // sentinel: not found

        while (lo <= hi)
        {
            int mid = lo + (hi - lo) / 2;
            if (_docIds[mid] >= targetDocId)
            {
                best = mid;
                hi = mid - 1;
            }
            else
            {
                lo = mid + 1;
            }
        }

        if (best < _count)
        {
            _index = best;
            return true;
        }

        _index = _count;
        return false;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (_docIds is not null)
        {
            ArrayPool<int>.Shared.Return(_docIds);
            _docIds = null;
        }
        if (_freqs is not null)
        {
            ArrayPool<int>.Shared.Return(_freqs);
            _freqs = null;
        }
    }

    public static PostingsEnum Empty => new(null, null, 0);
}
