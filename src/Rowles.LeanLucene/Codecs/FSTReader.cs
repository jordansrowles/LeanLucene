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

    // Hash table for O(1) average-case exact term lookups
    private readonly int[] _hashBuckets;
    private readonly int[] _hashNext;
    private readonly int _hashMask;

    private FSTReader(long[] offsets, int[] keyStarts, byte[] keyData, int termCount,
        int[] hashBuckets, int[] hashNext, int hashMask)
    {
        _offsets = offsets;
        _keyStarts = keyStarts;
        _keyData = keyData;
        _termCount = termCount;
        _hashBuckets = hashBuckets;
        _hashNext = hashNext;
        _hashMask = hashMask;
    }

    /// <summary>
    /// Opens a v2 dictionary from an <see cref="IndexInput"/> positioned just after the codec header.
    /// </summary>
    public static FSTReader Open(IndexInput input)
    {
        int termCount = input.ReadInt32();
        if (termCount == 0)
            return new FSTReader([], [], [], 0, [-1], [], 0);

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

        // Build hash table for O(1) exact lookups
        int capacity = 1;
        while (capacity < termCount * 2) capacity <<= 1;
        int hashMask = capacity - 1;
        var hashBuckets = new int[capacity];
        var hashNext = new int[termCount];
        Array.Fill(hashBuckets, -1);
        Array.Fill(hashNext, -1);
        for (int i = 0; i < termCount; i++)
        {
            int h = HashBytes(keyData.AsSpan(keyStarts[i], keyStarts[i + 1] - keyStarts[i])) & hashMask;
            hashNext[i] = hashBuckets[h];
            hashBuckets[h] = i;
        }

        return new FSTReader(offsets, keyStarts, keyData, termCount, hashBuckets, hashNext, hashMask);
    }

    /// <summary>O(1) average-case hash lookup on UTF-8 byte keys (falls back to chain walk on collision).</summary>
    public bool TryGetPostingsOffset(ReadOnlySpan<byte> termUtf8, out long offset)
    {
        offset = 0;
        int h = HashBytes(termUtf8) & _hashMask;
        int idx = _hashBuckets[h];
        while (idx >= 0)
        {
            if (GetKeySpan(idx).SequenceEqual(termUtf8))
            {
                offset = _offsets[idx];
                return true;
            }
            idx = _hashNext[idx];
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

        // Extract the non-wildcard literal prefix for a tighter lower bound scan.
        int literalEnd = 0;
        while (literalEnd < pattern.Length && pattern[literalEnd] != '*' && pattern[literalEnd] != '?')
            literalEnd++;

        int scanPrefixLen;
        int literalByteCount = 0;
        byte[]? scanBuf = null;
        if (literalEnd > 0)
        {
            var literalSpan = pattern[..literalEnd];
            literalByteCount = Encoding.UTF8.GetByteCount(literalSpan);
            scanPrefixLen = prefixByteCount + literalByteCount;
            scanBuf = new byte[scanPrefixLen];
            prefixUtf8.CopyTo(scanBuf);
            Encoding.UTF8.GetBytes(literalSpan, scanBuf.AsSpan(prefixByteCount));
        }
        else
        {
            scanPrefixLen = prefixByteCount;
        }

        ReadOnlySpan<byte> scanPrefix = scanBuf is not null ? scanBuf.AsSpan(0, scanPrefixLen) : prefixUtf8;

        var results = new List<(string, long)>();
        int start = LowerBound(scanPrefix);

        for (int i = start; i < _termCount; i++)
        {
            var key = GetKeySpan(i);
            if (!key.StartsWith(prefixUtf8))
                break;

            // Only decode terms whose bytes match the literal prefix
            if (scanBuf is not null && !key.StartsWith(scanPrefix))
                break;

            var fullTerm = DecodeKey(i);
            var bareTerm = fullTerm.AsSpan(fieldPrefix.Length);
            if (WildcardQuery.Matches(bareTerm, pattern))
                results.Add((fullTerm, _offsets[i]));
        }
        return results;
    }

    /// <summary>Returns all terms within Levenshtein distance for a field, with edit distances.</summary>
    public List<(string Term, long Offset, int Distance)> GetFuzzyMatches(string fieldPrefix, ReadOnlySpan<char> queryTerm, int maxEdits)
    {
        int prefixByteCount = Encoding.UTF8.GetByteCount(fieldPrefix);
        Span<byte> prefixUtf8 = prefixByteCount <= 256 ? stackalloc byte[prefixByteCount] : new byte[prefixByteCount];
        Encoding.UTF8.GetBytes(fieldPrefix, prefixUtf8);

        // Encode query term to UTF-8 for byte-level Levenshtein (avoids DecodeKey allocation)
        int queryByteCount = Encoding.UTF8.GetByteCount(queryTerm);
        Span<byte> queryUtf8 = queryByteCount <= 256 ? stackalloc byte[queryByteCount] : new byte[queryByteCount];
        Encoding.UTF8.GetBytes(queryTerm, queryUtf8);
        bool queryIsAscii = queryByteCount == queryTerm.Length;

        var results = new List<(string, long, int)>();
        int start = LowerBound(prefixUtf8);
        int queryTermLen = queryTerm.Length;

        for (int i = start; i < _termCount; i++)
        {
            var key = GetKeySpan(i);
            if (!key.StartsWith(prefixUtf8))
                break;

            int bareByteLen = key.Length - prefixByteCount;
            if (bareByteLen - maxEdits > queryTermLen || queryTermLen - maxEdits > bareByteLen)
                continue;

            // Try ASCII byte-level Levenshtein to avoid string allocation
            if (queryIsAscii)
            {
                var bareBytes = key[prefixByteCount..];
                int dist = LevenshteinDistance.ComputeAsciiBounded(queryUtf8, bareBytes, maxEdits);
                if (dist >= 0)
                {
                    // ASCII path succeeded
                    if (dist <= maxEdits)
                        results.Add((DecodeKey(i), _offsets[i], dist));
                    continue;
                }
                // Non-ASCII term — fall through to char-based path
            }

            var fullTerm = DecodeKey(i);
            var bareTerm = fullTerm.AsSpan(fieldPrefix.Length);

            if (Math.Abs(bareTerm.Length - queryTermLen) > maxEdits)
                continue;

            int distance = LevenshteinDistance.ComputeBounded(queryTerm, bareTerm, maxEdits);
            if (distance <= maxEdits)
                results.Add((fullTerm, _offsets[i], distance));
        }
        return results;
    }

    /// <summary>Returns all terms for a field (all terms with prefix "field\0").</summary>
    public List<(string Term, long Offset)> GetAllTermsForField(string fieldPrefix)
    {
        return GetTermsWithPrefix(fieldPrefix.AsSpan());
    }

    /// <summary>Enumerates all terms and their postings offsets in sorted order.</summary>
    public List<(string Term, long Offset)> EnumerateAllTerms()
    {
        var results = new List<(string, long)>(_termCount);
        for (int i = 0; i < _termCount; i++)
        {
            int start = _keyStarts[i];
            int len = _keyStarts[i + 1] - start;
            string term = System.Text.Encoding.UTF8.GetString(_keyData, start, len);
            results.Add((term, _offsets[i]));
        }
        return results;
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

        // Extract literal prefix from the regex pattern for byte-level gating.
        var pattern = regex.ToString().AsSpan();
        if (pattern.Length > 0 && pattern[0] == '^')
            pattern = pattern[1..];
        int litEnd = 0;
        while (litEnd < pattern.Length && !IsRegexMeta(pattern[litEnd]))
            litEnd++;

        int scanPrefixLen;
        byte[]? scanBuf = null;
        if (litEnd > 0)
        {
            var literalSpan = pattern[..litEnd];
            int literalByteCount = Encoding.UTF8.GetByteCount(literalSpan);
            scanPrefixLen = prefixByteCount + literalByteCount;
            scanBuf = new byte[scanPrefixLen];
            prefixUtf8.CopyTo(scanBuf);
            Encoding.UTF8.GetBytes(literalSpan, scanBuf.AsSpan(prefixByteCount));
        }
        else
        {
            scanPrefixLen = prefixByteCount;
        }

        ReadOnlySpan<byte> scanPrefix = scanBuf is not null ? scanBuf.AsSpan(0, scanPrefixLen) : prefixUtf8;

        var results = new List<(string, long)>();
        int start = LowerBound(scanPrefix);

        for (int i = start; i < _termCount; i++)
        {
            var key = GetKeySpan(i);
            if (!key.StartsWith(prefixUtf8))
                break;

            if (scanBuf is not null && !key.StartsWith(scanPrefix))
                break;

            var fullTerm = DecodeKey(i);
            var bareTerm = fullTerm.AsSpan(fieldPrefix.Length);
            if (regex.IsMatch(bareTerm))
                results.Add((fullTerm, _offsets[i]));
        }
        return results;
    }

    private static bool IsRegexMeta(char c) =>
        c is '.' or '*' or '+' or '?' or '[' or '(' or '{' or '|' or '\\' or '^' or '$';

    /// <summary>FNV-1a hash of a byte span.</summary>
    private static int HashBytes(ReadOnlySpan<byte> data)
    {
        uint hash = 2166136261;
        foreach (byte b in data)
        {
            hash ^= b;
            hash *= 16777619;
        }
        return (int)(hash & 0x7FFFFFFF);
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
