using Rowles.LeanLucene.Search;
using Rowles.LeanLucene.Store;

namespace Rowles.LeanLucene.Codecs;

/// <summary>
/// Reads a .dic file produced by <see cref="TermDictionaryWriter"/>.
/// Uses memory-mapped I/O via <see cref="IndexInput"/> and zero-allocation
/// span-based term comparison during sequential scans.
/// </summary>
public sealed class TermDictionaryReader : IDisposable
{
    private readonly IndexInput _input;
    private readonly (string Term, long Offset)[] _skipIndex;
    private readonly long _dataEnd;
    private bool _disposed;

    private TermDictionaryReader(IndexInput input, (string Term, long Offset)[] skipIndex, long dataEnd)
    {
        _input = input;
        _skipIndex = skipIndex;
        _dataEnd = dataEnd;
    }

    public static TermDictionaryReader Open(string filePath)
    {
        var input = new IndexInput(filePath);

        int skipCount = input.ReadInt32();
        var skipIndex = new (string Term, long Offset)[skipCount];

        for (int i = 0; i < skipCount; i++)
        {
            int termLen = input.ReadInt32();
            string term = input.ReadUtf8String(termLen);
            long offset = input.ReadInt64();
            skipIndex[i] = (term, offset);
        }

        long dataEnd = input.Length;
        return new TermDictionaryReader(input, skipIndex, dataEnd);
    }

    public bool TryGetPostingsOffset(string term, out long offset)
    {
        return TryGetPostingsOffset(term.AsSpan(), out offset);
    }

    /// <summary>
    /// Looks up a term in the dictionary. The sequential scan compares raw UTF-8
    /// bytes directly against the memory-mapped buffer — zero heap allocations.
    /// </summary>
    public bool TryGetPostingsOffset(ReadOnlySpan<char> term, out long offset)
    {
        offset = 0;

        int lo = 0, hi = _skipIndex.Length - 1;
        int bestBlock = -1;

        while (lo <= hi)
        {
            int mid = lo + (hi - lo) / 2;
            int cmp = _skipIndex[mid].Term.AsSpan().SequenceCompareTo(term);
            if (cmp <= 0)
            {
                bestBlock = mid;
                lo = mid + 1;
            }
            else
            {
                hi = mid - 1;
            }
        }

        if (bestBlock < 0)
            return false;

        long scanStart = _skipIndex[bestBlock].Offset;
        long scanEnd = bestBlock + 1 < _skipIndex.Length
            ? _skipIndex[bestBlock + 1].Offset
            : _dataEnd;

        // Encode search term as UTF-8 once for byte-level comparison in the scan loop
        Span<byte> termUtf8Buf = term.Length <= 128
            ? stackalloc byte[term.Length * 3]
            : new byte[term.Length * 3];
        int termUtf8Len = System.Text.Encoding.UTF8.GetBytes(term, termUtf8Buf);
        var termUtf8 = termUtf8Buf[..termUtf8Len];

        _input.Seek(scanStart);

        while (_input.Position < scanEnd)
        {
            int termLen = _input.ReadInt32();
            int cmp = _input.CompareUtf8BytesAndAdvance(termLen, termUtf8);
            long postingsOffset = _input.ReadInt64();

            if (cmp == 0)
            {
                offset = postingsOffset;
                return true;
            }
            if (cmp > 0)
                return false;
        }

        return false;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _input.Dispose();
    }

    /// <summary>
    /// Enumerates all terms with a given qualified prefix (field\0prefix).
    /// Returns (qualifiedTerm, postingsOffset) pairs.
    /// </summary>
    public List<(string Term, long Offset)> GetTermsWithPrefix(ReadOnlySpan<char> qualifiedPrefix)
    {
        var results = new List<(string, long)>();

        Span<byte> prefixUtf8Buf = qualifiedPrefix.Length <= 128
            ? stackalloc byte[qualifiedPrefix.Length * 3]
            : new byte[qualifiedPrefix.Length * 3];
        int prefixUtf8Len = System.Text.Encoding.UTF8.GetBytes(qualifiedPrefix, prefixUtf8Buf);
        var prefixUtf8 = prefixUtf8Buf[..prefixUtf8Len];

        // Binary search for the first skip block that could contain the prefix
        int lo = 0, hi = _skipIndex.Length - 1;
        int bestBlock = -1;
        while (lo <= hi)
        {
            int mid = lo + (hi - lo) / 2;
            int cmp = _skipIndex[mid].Term.AsSpan().SequenceCompareTo(qualifiedPrefix);
            if (cmp <= 0)
            {
                bestBlock = mid;
                lo = mid + 1;
            }
            else
            {
                hi = mid - 1;
            }
        }

        long scanStart = bestBlock >= 0 ? _skipIndex[bestBlock].Offset : (_skipIndex.Length > 0 ? _skipIndex[0].Offset : 0);
        _input.Seek(scanStart);

        while (_input.Position < _dataEnd)
        {
            int termLen = _input.ReadInt32();
            byte[] termBytes = _input.ReadBytes(termLen);
            long postingsOffset = _input.ReadInt64();

            // Check if term starts with prefix
            if (termBytes.Length >= prefixUtf8Len &&
                termBytes.AsSpan(0, prefixUtf8Len).SequenceEqual(prefixUtf8))
            {
                string term = System.Text.Encoding.UTF8.GetString(termBytes);
                results.Add((term, postingsOffset));
            }
            else if (termBytes.AsSpan().SequenceCompareTo(prefixUtf8) > 0 && results.Count > 0)
            {
                // Past the prefix range in sorted order
                break;
            }
        }

        return results;
    }

    /// <summary>
    /// Enumerates all terms matching a wildcard pattern for a given field.
    /// The qualifiedPrefix should be "field\0" to scope to a specific field.
    /// </summary>
    public List<(string Term, long Offset)> GetTermsMatching(string fieldPrefix, ReadOnlySpan<char> pattern)
    {
        var results = new List<(string, long)>();
        var allTerms = GetAllTermsForField(fieldPrefix);

        foreach (var (qualifiedTerm, offset) in allTerms)
        {
            // Extract the term part after the field\0 prefix
            var term = qualifiedTerm.AsSpan(fieldPrefix.Length);
            if (WildcardQuery.Matches(term, pattern))
                results.Add((qualifiedTerm, offset));
        }

        return results;
    }

    /// <summary>
    /// Enumerates all terms for a given field (prefix "field\0").
    /// </summary>
    public List<(string Term, long Offset)> GetAllTermsForField(string fieldPrefix)
    {
        return GetTermsWithPrefix(fieldPrefix.AsSpan());
    }
}
