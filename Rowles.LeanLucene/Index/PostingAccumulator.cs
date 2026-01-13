namespace Rowles.LeanLucene.Index;

/// <summary>
/// Accumulates doc IDs, term frequencies, and positions for a single qualified term
/// during indexing. Replaces the triple-dictionary approach with a single flat structure.
/// </summary>
internal sealed class PostingAccumulator
{
    private int[] _docIds;
    private int[] _freqs;
    private int[][] _positions;
    private byte[]?[][]? _payloads; // lazily allocated when payloads are used
    private int _count;

    public PostingAccumulator()
    {
        _docIds = new int[4];
        _freqs = new int[4];
        _positions = new int[4][];
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
            // Same doc — increment freq and append position
            _freqs[_count - 1]++;
            ref var posArr = ref _positions[_count - 1];
            int posLen = posArr.Length;
            int posCount = _freqs[_count - 1];
            if (posCount > posLen)
            {
                Array.Resize(ref posArr, posLen * 2);
            }
            posArr[posCount - 1] = position;
            return;
        }

        // New doc
        if (_count == _docIds.Length)
            Grow();

        _docIds[_count] = docId;
        _freqs[_count] = 1;
        _positions[_count] = new int[4];
        _positions[_count][0] = position;
        _count++;
    }

    /// <summary>Adds a posting with an optional payload byte array.</summary>
    public void AddWithPayload(int docId, int position, byte[]? payload)
    {
        if (_payloads == null)
        {
            _payloads = new byte[]?[_docIds.Length][];
            for (int i = 0; i < _count; i++)
                _payloads[i] = new byte[]?[_positions[i].Length];
        }

        if (_count > 0 && _docIds[_count - 1] == docId)
        {
            _freqs[_count - 1]++;
            ref var posArr = ref _positions[_count - 1];
            int posCount = _freqs[_count - 1];
            if (posCount > posArr.Length)
            {
                Array.Resize(ref posArr, posArr.Length * 2);
                Array.Resize(ref _payloads[_count - 1], posArr.Length);
            }
            posArr[posCount - 1] = position;
            _payloads[_count - 1][posCount - 1] = payload;
            return;
        }

        if (_count == _docIds.Length)
            Grow();

        _docIds[_count] = docId;
        _freqs[_count] = 1;
        _positions[_count] = new int[4];
        _positions[_count][0] = position;
        _payloads[_count] = new byte[]?[4];
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
        _positions[_count] = [];
        _count++;
    }

    public ReadOnlySpan<int> DocIds => _docIds.AsSpan(0, _count);

    public int GetFreq(int index) => _freqs[index];

    public ReadOnlySpan<int> GetPositions(int index) => _positions[index].AsSpan(0, _freqs[index]);

    /// <summary>Gets the payload for a specific position index of a given posting entry.</summary>
    public byte[]? GetPayload(int docIndex, int positionIndex)
        => _payloads != null && docIndex < _count && _payloads[docIndex] != null
            ? _payloads[docIndex][positionIndex]
            : null;

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
                if (_freqs[i] > 0) return true;
            return false;
        }
    }

    private void Grow()
    {
        int newLen = _docIds.Length * 2;
        Array.Resize(ref _docIds, newLen);
        Array.Resize(ref _freqs, newLen);
        Array.Resize(ref _positions, newLen);
        if (_payloads != null)
            Array.Resize(ref _payloads, newLen);
    }
}
