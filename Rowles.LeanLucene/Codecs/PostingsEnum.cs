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

        var docIds = ArrayPool<int>.Shared.Rent(count);
        int prev = 0;
        for (int i = 0; i < count; i++)
        {
            prev += input.ReadInt32();
            docIds[i] = prev;
        }

        bool hasFreqs = input.ReadBoolean();
        int[]? freqs = null;
        if (hasFreqs)
        {
            freqs = ArrayPool<int>.Shared.Rent(count);
            for (int i = 0; i < count; i++)
                freqs[i] = input.ReadInt32();
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
