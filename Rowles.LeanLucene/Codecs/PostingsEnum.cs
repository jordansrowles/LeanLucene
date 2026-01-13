using System.Buffers;
using Rowles.LeanLucene.Store;

namespace Rowles.LeanLucene.Codecs;

/// <summary>
/// Forward-only cursor over a postings list. Decodes doc IDs and frequencies
/// once into ArrayPool-rented buffers, then yields (DocId, Freq) pairs via MoveNext().
/// Optionally decodes positions when created via <see cref="CreateWithPositions"/>.
/// </summary>
public struct PostingsEnum : IDisposable
{
    private int[]? _docIds;
    private int[]? _freqs;
    private readonly int _count;
    private int _index;
    private bool _disposed;
    private int[]? _positionData;
    private int[]? _positionStarts;

    public int DocFreq => _count;
    public int DocId => _index >= 0 && _index < _count ? _docIds![_index] : -1;
    public int Freq => _index >= 0 && _index < _count && _freqs is not null ? _freqs[_index] : 1;
    public bool IsExhausted => _count == 0;

    private PostingsEnum(int[]? docIds, int[]? freqs, int count,
        int[]? positionData = null, int[]? positionStarts = null)
    {
        _docIds = docIds;
        _freqs = freqs;
        _count = count;
        _index = -1;
        _disposed = false;
        _positionData = positionData;
        _positionStarts = positionStarts;
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

    /// <summary>
    /// Creates a PostingsEnum that also decodes position data for phrase queries.
    /// Positions are decoded upfront into flat ArrayPool buffers for O(1) per-doc access.
    /// </summary>
    public static PostingsEnum CreateWithPositions(IndexInput input, long offset)
    {
        input.Seek(offset);
        int count = input.ReadInt32();
        if (count <= 0)
            return Empty;

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

        bool hasPositions = input.ReadBoolean();
        int[]? positionData = null;
        int[]? positionStarts = null;
        if (hasPositions)
        {
            positionStarts = ArrayPool<int>.Shared.Rent(count + 1);
            int estimatedTotal = count * 4;
            positionData = ArrayPool<int>.Shared.Rent(estimatedTotal);
            int totalPositions = 0;

            for (int i = 0; i < count; i++)
            {
                positionStarts[i] = totalPositions;
                int posCount = input.ReadVarInt();

                if (totalPositions + posCount > positionData.Length)
                {
                    var newData = ArrayPool<int>.Shared.Rent(
                        Math.Max(positionData.Length * 2, totalPositions + posCount));
                    positionData.AsSpan(0, totalPositions).CopyTo(newData);
                    ArrayPool<int>.Shared.Return(positionData);
                    positionData = newData;
                }

                int prevPos = 0;
                for (int j = 0; j < posCount; j++)
                {
                    prevPos += input.ReadVarInt();
                    positionData[totalPositions++] = prevPos;
                }
            }
            positionStarts[count] = totalPositions;
        }

        return new PostingsEnum(docIds, freqs, count, positionData, positionStarts);
    }

    /// <summary>
    /// Returns positions for the current document. Only available when created via
    /// <see cref="CreateWithPositions"/>. Returns empty span if positions were not decoded.
    /// </summary>
    public readonly ReadOnlySpan<int> GetCurrentPositions()
    {
        if (_positionData is null || _positionStarts is null || _index < 0 || _index >= _count)
            return ReadOnlySpan<int>.Empty;
        int start = _positionStarts[_index];
        int end = _positionStarts[_index + 1];
        return new ReadOnlySpan<int>(_positionData, start, end - start);
    }

    /// <summary>
    /// Gets the payload for a specific position index of the current document.
    /// Returns empty span when no payloads are stored.
    /// </summary>
    public readonly ReadOnlySpan<byte> GetPayload(int positionIndex)
    {
        // Payload data is not yet stored in the postings format;
        // this stub allows consumers to compile and will return data once
        // the binary format is extended with per-position payloads.
        return ReadOnlySpan<byte>.Empty;
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
        if (_positionData is not null)
        {
            ArrayPool<int>.Shared.Return(_positionData);
            _positionData = null;
        }
        if (_positionStarts is not null)
        {
            ArrayPool<int>.Shared.Return(_positionStarts);
            _positionStarts = null;
        }
    }

    public static PostingsEnum Empty => new(null, null, 0);
}
