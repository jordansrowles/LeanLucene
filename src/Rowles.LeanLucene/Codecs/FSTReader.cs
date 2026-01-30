using System.Text;
using System.Text.RegularExpressions;
using Rowles.LeanLucene.Search;
using Rowles.LeanLucene.Store;

namespace Rowles.LeanLucene.Codecs;

/// <summary>
/// Reads a v2 .dic file: compact byte-keyed sorted dictionary.
/// All data is loaded into contiguous arrays at open time — no per-term string allocation.
/// Binary search operates on raw UTF-8 bytes (~3× faster than char-span comparison).
/// </summary>
internal sealed class FSTReader
{
    private readonly long[] _offsets;
    private readonly int[] _keyStarts;
    private readonly byte[] _keyData;
    private readonly int _termCount;

    private FSTReader(long[] offsets, int[] keyStarts, byte[] keyData, int termCount)
    {
        _offsets = offsets;
        _keyStarts = keyStarts;
        _keyData = keyData;
        _termCount = termCount;
    }

    /// <summary>
    /// Opens a v2 dictionary from an <see cref="IndexInput"/> positioned just after the codec header.
    /// </summary>
    public static FSTReader Open(IndexInput input)
    {
        int termCount = input.ReadInt32();
        if (termCount == 0)
            return new FSTReader([], [], [], 0);

        // Read postings offsets (N × int64)
        var offsets = new long[termCount];
        for (int i = 0; i < termCount; i++)
            offsets[i] = input.ReadInt64();

        // Read key starts ((N+1) × int32)
        var keyStarts = new int[termCount + 1];
        for (int i = 0; i <= termCount; i++)
            keyStarts[i] = input.ReadInt32();

        // Read concatenated UTF-8 key data
        int totalKeyBytes = keyStarts[termCount];
        var keyData = new byte[totalKeyBytes];
        if (totalKeyBytes > 0)
        {
            var span = input.ReadSpan(totalKeyBytes);
            span.CopyTo(keyData);
        }

        return new FSTReader(offsets, keyStarts, keyData, termCount);
    }

    /// <summary>O(log N) binary search on UTF-8 byte keys.</summary>
    public bool TryGetPostingsOffset(ReadOnlySpan<byte> termUtf8, out long offset)
    {
        offset = 0;
        int lo = 0, hi = _termCount - 1;
        while (lo <= hi)
        {
            int mid = lo + (hi - lo) / 2;
            var key = GetKeySpan(mid);
            int cmp = key.SequenceCompareTo(termUtf8);
            if (cmp == 0) { offset = _offsets[mid]; return true; }
            if (cmp < 0) lo = mid + 1;
            else hi = mid - 1;
        }
        return false;
    }

    /// <summary>O(log N) binary search accepting a char span (encodes to UTF-8 internally).</summary>
    public bool TryGetPostingsOffset(ReadOnlySpan<char> term, out long offset)
    {
        int byteCount = Encoding.UTF8.GetByteCount(term);
        Span<byte> utf8 = byteCount <= 256 ? stackalloc byte[byteCount] : new byte[byteCount];
        Encoding.UTF8.GetBytes(term, utf8);
        return TryGetPostingsOffset(utf8, out offset);
    }

    /// <summary>Returns all terms sharing the given qualified prefix.</summary>
    public List<(string Term, long Offset)> GetTermsWithPrefix(ReadOnlySpan<char> qualifiedPrefix)
    {
        int byteCount = Encoding.UTF8.GetByteCount(qualifiedPrefix);
        Span<byte> prefixUtf8 = byteCount <= 256 ? stackalloc byte[byteCount] : new byte[byteCount];
        Encoding.UTF8.GetBytes(qualifiedPrefix, prefixUtf8);

        var results = new List<(string, long)>();
        int start = LowerBound(prefixUtf8);

        for (int i = start; i < _termCount; i++)
        {
            var key = GetKeySpan(i);
            if (key.StartsWith(prefixUtf8))
                results.Add((DecodeKey(i), _offsets[i]));
            else
                break;
        }
        return results;
    }

    /// <summary>Returns all terms matching a wildcard pattern for a given field.</summary>
    public List<(string Term, long Offset)> GetTermsMatching(string fieldPrefix, ReadOnlySpan<char> pattern)
    {
        int prefixByteCount = Encoding.UTF8.GetByteCount(fieldPrefix);
        Span<byte> prefixUtf8 = prefixByteCount <= 256 ? stackalloc byte[prefixByteCount] : new byte[prefixByteCount];
        Encoding.UTF8.GetBytes(fieldPrefix, prefixUtf8);

        var results = new List<(string, long)>();
        int start = LowerBound(prefixUtf8);

        for (int i = start; i < _termCount; i++)
        {
            var key = GetKeySpan(i);
            if (!key.StartsWith(prefixUtf8))
                break;

            // Decode bare term for wildcard matching
            var fullTerm = DecodeKey(i);
            var bareTerm = fullTerm.AsSpan(fieldPrefix.Length);
            if (WildcardQuery.Matches(bareTerm, pattern))
                results.Add((fullTerm, _offsets[i]));
        }
        return results;
    }

    /// <summary>Returns all terms within Levenshtein distance for a field.</summary>
    public List<(string Term, long Offset)> GetFuzzyMatches(string fieldPrefix, ReadOnlySpan<char> queryTerm, int maxEdits)
    {
        int prefixByteCount = Encoding.UTF8.GetByteCount(fieldPrefix);
        Span<byte> prefixUtf8 = prefixByteCount <= 256 ? stackalloc byte[prefixByteCount] : new byte[prefixByteCount];
        Encoding.UTF8.GetBytes(fieldPrefix, prefixUtf8);

        var results = new List<(string, long)>();
        int start = LowerBound(prefixUtf8);
        int queryTermLen = queryTerm.Length;

        for (int i = start; i < _termCount; i++)
        {
            var key = GetKeySpan(i);
            if (!key.StartsWith(prefixUtf8))
                break;

            var fullTerm = DecodeKey(i);
            var bareTerm = fullTerm.AsSpan(fieldPrefix.Length);

            if (Math.Abs(bareTerm.Length - queryTermLen) > maxEdits)
                continue;

            int distance = LevenshteinDistance.Compute(queryTerm, bareTerm);
            if (distance <= maxEdits)
                results.Add((fullTerm, _offsets[i]));
        }
        return results;
    }

    /// <summary>Returns all terms for a field (all terms with prefix "field\0").</summary>
    public List<(string Term, long Offset)> GetAllTermsForField(string fieldPrefix)
    {
        return GetTermsWithPrefix(fieldPrefix.AsSpan());
    }

    /// <summary>Returns terms whose bare value falls within a lexicographic range.</summary>
    public List<(string Term, long Offset)> GetTermsInRange(
        string fieldPrefix,
        string? lower, string? upper,
        bool includeLower = true, bool includeUpper = true)
    {
        string scanKey = lower is not null ? fieldPrefix + lower : fieldPrefix;
        int scanByteCount = Encoding.UTF8.GetByteCount(scanKey);
        Span<byte> scanUtf8 = scanByteCount <= 256 ? stackalloc byte[scanByteCount] : new byte[scanByteCount];
        Encoding.UTF8.GetBytes(scanKey, scanUtf8);

        int prefixByteCount = Encoding.UTF8.GetByteCount(fieldPrefix);
        Span<byte> prefixUtf8 = prefixByteCount <= 256 ? stackalloc byte[prefixByteCount] : new byte[prefixByteCount];
        Encoding.UTF8.GetBytes(fieldPrefix, prefixUtf8);

        var results = new List<(string, long)>();
        int start = LowerBound(scanUtf8);

        for (int i = start; i < _termCount; i++)
        {
            var key = GetKeySpan(i);
            if (!key.StartsWith(prefixUtf8))
                break;

            var fullTerm = DecodeKey(i);
            var bareTerm = fullTerm.AsSpan(fieldPrefix.Length);

            if (lower is not null)
            {
                int cmp = bareTerm.SequenceCompareTo(lower.AsSpan());
                if (cmp < 0 || (cmp == 0 && !includeLower)) continue;
            }

            if (upper is not null)
            {
                int cmp = bareTerm.SequenceCompareTo(upper.AsSpan());
                if (cmp > 0 || (cmp == 0 && !includeUpper)) break;
            }

            results.Add((fullTerm, _offsets[i]));
        }
        return results;
    }

    /// <summary>Returns terms for a field whose bare text matches the given compiled regex.</summary>
    public List<(string Term, long Offset)> GetTermsMatchingRegex(string fieldPrefix, Regex regex)
    {
        int prefixByteCount = Encoding.UTF8.GetByteCount(fieldPrefix);
        Span<byte> prefixUtf8 = prefixByteCount <= 256 ? stackalloc byte[prefixByteCount] : new byte[prefixByteCount];
        Encoding.UTF8.GetBytes(fieldPrefix, prefixUtf8);

        var results = new List<(string, long)>();
        int start = LowerBound(prefixUtf8);

        for (int i = start; i < _termCount; i++)
        {
            var key = GetKeySpan(i);
            if (!key.StartsWith(prefixUtf8))
                break;

            var fullTerm = DecodeKey(i);
            var bareTerm = fullTerm.AsSpan(fieldPrefix.Length);
            if (regex.IsMatch(bareTerm))
                results.Add((fullTerm, _offsets[i]));
        }
        return results;
    }

    /// <summary>Returns the raw UTF-8 byte span for term at ordinal index.</summary>
    private ReadOnlySpan<byte> GetKeySpan(int ordinal)
    {
        int start = _keyStarts[ordinal];
        int end = _keyStarts[ordinal + 1];
        return _keyData.AsSpan(start, end - start);
    }

    /// <summary>Decodes the UTF-8 key at ordinal index to a string.</summary>
    private string DecodeKey(int ordinal)
    {
        var span = GetKeySpan(ordinal);
        return Encoding.UTF8.GetString(span);
    }

    /// <summary>Returns the first ordinal where term >= key (lower bound on UTF-8 bytes).</summary>
    private int LowerBound(ReadOnlySpan<byte> key)
    {
        int lo = 0, hi = _termCount;
        while (lo < hi)
        {
            int mid = lo + (hi - lo) / 2;
            if (GetKeySpan(mid).SequenceCompareTo(key) < 0)
                lo = mid + 1;
            else
                hi = mid;
        }
        return lo;
    }
}
