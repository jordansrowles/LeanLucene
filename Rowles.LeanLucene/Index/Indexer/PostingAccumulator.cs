using System.Buffers;

namespace Rowles.LeanLucene.Index.Indexer;

/// <summary>
/// Accumulates doc IDs, term frequencies, and positions for a single qualified term
/// during indexing. Uses a single flat position buffer to avoid per-posting small array allocations.
/// </summary>
internal sealed class PostingAccumulator
{
    private int[] _docIds;
    private int[] _freqs;
    // Per-posting index into flat position buffer: (start offset, allocated length)
    private int[] _posStarts;
    private int[] _posLengths;
    // Single flat buffer for ALL positions across all postings
    private int[] _posBuf;
    private int _posBufUsed;
    private byte[]?[][]? _payloads; // lazily allocated when payloads are used
    private int _count;

    private const int NoPositionSentinel = -1;

    public PostingAccumulator()
    {
        _docIds = new int[4];
        _freqs = new int[4];
        _posStarts = new int[4];
        _posLengths = new int[4];
        _posBuf = new int[8]; // shared flat buffer for all positions
        _posBufUsed = 0;
        _count = 0;
    }

    public int Count => _count;

    /// <summary>
    /// Adds or updates a posting for the given doc ID at the given position.
    /// </summary>
    public void Add(int docId, int position)
    {
        if (_count > 0 && _docIds[_count - 1] == docId)
        {
            // Same doc — increment freq and append position to flat buffer
            _freqs[_count - 1]++;
            int freq = _freqs[_count - 1];
            int start = _posStarts[_count - 1];
            int allocated = _posLengths[_count - 1];
            if (freq > allocated)
            {
                // Need more space: grow the slot in the flat buffer
                int newAllocated = allocated * 2;
                EnsurePosBufCapacity(_posBufUsed + newAllocated);
                // Copy existing positions to end of buffer
                int newStart = _posBufUsed;
                Array.Copy(_posBuf, start, _posBuf, newStart, freq - 1);
                _posStarts[_count - 1] = newStart;
                _posLengths[_count - 1] = newAllocated;
                _posBuf[newStart + freq - 1] = position;
                _posBufUsed += newAllocated;
            }
            else
            {
                _posBuf[start + freq - 1] = position;
            }
            return;
        }

        // New doc
        if (_count == _docIds.Length)
            Grow();

        EnsurePosBufCapacity(_posBufUsed + 1);
        _docIds[_count] = docId;
        _freqs[_count] = 1;
        _posStarts[_count] = _posBufUsed;
        _posLengths[_count] = 1;
        _posBuf[_posBufUsed] = position;
        _posBufUsed += 1;
        _count++;
    }

    /// <summary>Adds a posting with an optional payload byte array.</summary>
    public void AddWithPayload(int docId, int position, byte[]? payload)
    {
        if (_payloads == null)
        {
            _payloads = new byte[]?[_docIds.Length][];
            for (int i = 0; i < _count; i++)
            {
                int freq = _freqs[i] > 0 ? _freqs[i] : 0;
                _payloads[i] = new byte[]?[freq];
            }
        }

        if (_count > 0 && _docIds[_count - 1] == docId)
        {
            _freqs[_count - 1]++;
            int freq = _freqs[_count - 1];
            int start = _posStarts[_count - 1];
            int allocated = _posLengths[_count - 1];
            if (freq > allocated)
            {
                int newAllocated = allocated * 2;
                EnsurePosBufCapacity(_posBufUsed + newAllocated);
                int newStart = _posBufUsed;
                Array.Copy(_posBuf, start, _posBuf, newStart, freq - 1);
                _posStarts[_count - 1] = newStart;
                _posLengths[_count - 1] = newAllocated;
                _posBuf[newStart + freq - 1] = position;
                _posBufUsed += newAllocated;
                Array.Resize(ref _payloads[_count - 1], newAllocated);
            }
            else
            {
                _posBuf[start + freq - 1] = position;
            }
            _payloads[_count - 1][freq - 1] = payload;
            return;
        }

        if (_count == _docIds.Length)
            Grow();

        EnsurePosBufCapacity(_posBufUsed + 1);
        _docIds[_count] = docId;
        _freqs[_count] = 1;
        _posStarts[_count] = _posBufUsed;
        _posLengths[_count] = 1;
        _posBuf[_posBufUsed] = position;
        _posBufUsed += 1;
        _payloads[_count] = new byte[]?[1];
        _payloads[_count][0] = payload;
        _count++;
    }

    /// <summary>
    /// Adds a doc ID without position (for string/keyword fields).
    /// </summary>
    public void AddDocOnly(int docId)
    {
        if (_count > 0 && _docIds[_count - 1] == docId)
            return;

        if (_count == _docIds.Length)
            Grow();

        _docIds[_count] = docId;
        _freqs[_count] = 0;
        _posStarts[_count] = NoPositionSentinel;
        _posLengths[_count] = 0;
        _count++;
    }

    public ReadOnlySpan<int> DocIds => _docIds.AsSpan(0, _count);

    public int GetFreq(int index) => _freqs[index];

    public ReadOnlySpan<int> GetPositions(int index)
    {
        int start = _posStarts[index];
        if (start == NoPositionSentinel) return ReadOnlySpan<int>.Empty;
        return _posBuf.AsSpan(start, _freqs[index]);
    }

    /// <summary>Gets the payload for a specific position index of a given posting entry.</summary>
    public byte[]? GetPayload(int docIndex, int positionIndex)
    {
        if (_payloads == null || (uint)docIndex >= (uint)_count || _payloads[docIndex] == null)
            return null;
        var docPayloads = _payloads[docIndex];
        if ((uint)positionIndex >= (uint)docPayloads.Length)
            throw new ArgumentOutOfRangeException(nameof(positionIndex),
                $"Position index {positionIndex} is out of range for doc entry with {docPayloads.Length} positions.");
        return docPayloads[positionIndex];
    }

    public bool HasPayloads => _payloads != null;

    public bool HasFreqs
    {
        get
        {
            for (int i = 0; i < _count; i++)
                if (_freqs[i] > 0) return true;
            return false;
        }
    }

    public bool HasPositions
    {
        get
        {
            for (int i = 0; i < _count; i++)
                if (_posStarts[i] != NoPositionSentinel && _freqs[i] > 0) return true;
            return false;
        }
    }

    private void Grow()
    {
        int newLen = _docIds.Length * 2;
        Array.Resize(ref _docIds, newLen);
        Array.Resize(ref _freqs, newLen);
        Array.Resize(ref _posStarts, newLen);
        Array.Resize(ref _posLengths, newLen);
        if (_payloads != null)
            Array.Resize(ref _payloads, newLen);
    }

    private void EnsurePosBufCapacity(int required)
    {
        if (required <= _posBuf.Length) return;
        int newLen = Math.Max(_posBuf.Length * 2, required);
        Array.Resize(ref _posBuf, newLen);
    }
}
