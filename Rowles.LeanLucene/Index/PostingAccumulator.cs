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
    }
}
