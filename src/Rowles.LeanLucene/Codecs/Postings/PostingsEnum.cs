using System.Buffers;
using System.Runtime.CompilerServices;
using Rowles.LeanLucene.Store;

namespace Rowles.LeanLucene.Codecs.Postings;

/// <summary>
/// Forward-only cursor over a postings list. Decodes doc IDs and frequencies
/// once into ArrayPool-rented buffers, then yields (DocId, Freq) pairs via MoveNext().
/// Optionally decodes positions when created via <see cref="CreateWithPositions"/>.
/// 
/// <para><b>Lifetime contract:</b> When using the lazy position path, this struct holds a raw
/// <c>byte*</c> pointer into a memory-mapped <see cref="IndexInput"/>. The source input
/// (<see cref="_sourceInput"/>) must remain open and un-disposed for the entire lifetime
/// of this PostingsEnum. Callers must not dispose the IndexInput while any PostingsEnum
/// referencing it is still alive.</para>
/// </summary>
public unsafe struct PostingsEnum : IDisposable
{
    private int[]? _docIds;
    private int[]? _freqs;
    private readonly int _count;
    private int _index;
    private bool _disposed;
    private int[]? _positionData;
    private int[]? _positionStarts;

    // Lazy position decoding: store per-doc byte offsets and base pointer
    private long[]? _positionByteOffsets;
    private int[]? _positionCounts;
    private byte* _posBasePtr;
    private int[]? _lazyPosBuffer;
    // Prevents the IndexInput from being disposed/GC'd while this PostingsEnum holds a raw pointer
    private IndexInput? _sourceInput;

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
        _positionByteOffsets = null;
        _positionCounts = null;
        _posBasePtr = null;
        _lazyPosBuffer = null;
        _sourceInput = null;
    }

    private PostingsEnum(int[]? docIds, int[]? freqs, int count,
        long[]? positionByteOffsets, int[]? positionCounts, byte* posBasePtr, IndexInput? sourceInput)
    {
        _docIds = docIds;
        _freqs = freqs;
        _count = count;
        _index = -1;
        _disposed = false;
        _positionData = null;
        _positionStarts = null;
        _positionByteOffsets = positionByteOffsets;
        _positionCounts = positionCounts;
        _posBasePtr = posBasePtr;
        _lazyPosBuffer = null;
        _sourceInput = sourceInput;
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
    /// Creates a PostingsEnum that lazily decodes position data for phrase queries.
    /// During creation, only per-doc byte offsets and position counts are recorded.
    /// Actual position values are decoded on-demand via <see cref="GetCurrentPositions"/>.
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
        if (!hasPositions)
            return new PostingsEnum(docIds, freqs, count);

        // Record per-doc byte offsets for lazy position decoding
        var positionByteOffsets = ArrayPool<long>.Shared.Rent(count);
        var positionCounts = ArrayPool<int>.Shared.Rent(count);

        for (int i = 0; i < count; i++)
        {
            int posCount = input.ReadVarInt();
            positionCounts[i] = posCount;
            positionByteOffsets[i] = input.Position;
            // Skip past the position VarInts without decoding values
            for (int j = 0; j < posCount; j++)
                input.ReadVarInt();
        }

        return new PostingsEnum(docIds, freqs, count, positionByteOffsets, positionCounts, input.BasePointer, input);
    }

    /// <summary>
    /// Returns positions for the current document. Supports both eager (pre-decoded) and
    /// lazy (on-demand) position data. Returns empty span if positions were not available.
    /// </summary>
    public ReadOnlySpan<int> GetCurrentPositions()
    {
        if (_disposed || _index < 0 || _index >= _count)
            return ReadOnlySpan<int>.Empty;

        // Eager path (pre-decoded positions)
        if (_positionData is not null && _positionStarts is not null)
        {
            int start = _positionStarts[_index];
            int end = _positionStarts[_index + 1];
            return new ReadOnlySpan<int>(_positionData, start, end - start);
        }

        // Lazy path: decode positions on-demand from mmap'd memory
        if (_positionByteOffsets is not null && _positionCounts is not null && _posBasePtr != null)
        {
            int posCount = _positionCounts[_index];
            if (posCount == 0)
                return ReadOnlySpan<int>.Empty;

            // Ensure buffer is large enough
            if (_lazyPosBuffer is null || _lazyPosBuffer.Length < posCount)
            {
                if (_lazyPosBuffer is not null)
                    ArrayPool<int>.Shared.Return(_lazyPosBuffer);
                _lazyPosBuffer = ArrayPool<int>.Shared.Rent(posCount);
            }

            // Decode VarInt position deltas directly from mmap'd memory
            long pos = _positionByteOffsets[_index];
            int prevPos = 0;
            for (int j = 0; j < posCount; j++)
            {
                int delta = ReadVarIntFromPtr(_posBasePtr, ref pos);
                prevPos += delta;
                _lazyPosBuffer[j] = prevPos;
            }

            return new ReadOnlySpan<int>(_lazyPosBuffer, 0, posCount);
        }

        return ReadOnlySpan<int>.Empty;
    }

    /// <summary>Reads a VarInt directly from a raw byte pointer at the given position.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int ReadVarIntFromPtr(byte* ptr, ref long position)
    {
        uint result = 0;
        int shift = 0;
        byte b;
        do
        {
            b = ptr[position++];
            result |= (uint)(b & 0x7F) << shift;
            shift += 7;
        } while ((b & 0x80) != 0);
        return (int)result;
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
        // STUB: payloads are accepted via PostingAccumulator.AddWithPayload but not yet persisted to disk format
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
        if (_positionByteOffsets is not null)
        {
            ArrayPool<long>.Shared.Return(_positionByteOffsets);
            _positionByteOffsets = null;
        }
        if (_positionCounts is not null)
        {
            ArrayPool<int>.Shared.Return(_positionCounts);
            _positionCounts = null;
        }
        if (_lazyPosBuffer is not null)
        {
            ArrayPool<int>.Shared.Return(_lazyPosBuffer);
            _lazyPosBuffer = null;
        }
    }

    public static PostingsEnum Empty => new(null, null, 0);

    /// <summary>
    /// Validates the postings file header. Should be called when opening a segment,
    /// before using Create/CreateWithPositions which seek to absolute term offsets.
    /// </summary>
    public static void ValidateFileHeader(IndexInput input)
    {
        input.Seek(0);
        CodecConstants.ValidateHeader(input, CodecConstants.PostingsVersion, "postings (.pos)");
    }
}
