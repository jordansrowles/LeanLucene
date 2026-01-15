using System.Text.RegularExpressions;
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
            var termSpan = _input.ReadSpan(termLen);
            long postingsOffset = _input.ReadInt64();

            // Check if term starts with prefix using zero-alloc span comparison
            if (termSpan.Length >= prefixUtf8Len &&
                termSpan[..prefixUtf8Len].SequenceEqual(prefixUtf8))
            {
                // Only allocate string for matching terms
                string term = System.Text.Encoding.UTF8.GetString(termSpan);
                results.Add((term, postingsOffset));
            }
            else if (termSpan.SequenceCompareTo(prefixUtf8) > 0 && results.Count > 0)
            {
                // Past the prefix range in sorted order
                break;
            }
        }

        return results;
    }

    /// <summary>
    /// Enumerates all terms matching a wildcard pattern for a given field.
    /// Scans the dictionary directly, only allocating strings for matching terms.
    /// </summary>
    public List<(string Term, long Offset)> GetTermsMatching(string fieldPrefix, ReadOnlySpan<char> pattern)
    {
        var results = new List<(string, long)>();

        Span<byte> prefixUtf8Buf = fieldPrefix.Length <= 128
            ? stackalloc byte[fieldPrefix.Length * 3]
            : new byte[fieldPrefix.Length * 3];
        int prefixUtf8Len = System.Text.Encoding.UTF8.GetBytes(fieldPrefix.AsSpan(), prefixUtf8Buf);
        var prefixUtf8 = prefixUtf8Buf[..prefixUtf8Len];

        int lo = 0, hi = _skipIndex.Length - 1;
        int bestBlock = -1;
        while (lo <= hi)
        {
            int mid = lo + (hi - lo) / 2;
            int cmp = _skipIndex[mid].Term.AsSpan().SequenceCompareTo(fieldPrefix.AsSpan());
            if (cmp <= 0) { bestBlock = mid; lo = mid + 1; }
            else hi = mid - 1;
        }

        long scanStart = bestBlock >= 0 ? _skipIndex[bestBlock].Offset : (_skipIndex.Length > 0 ? _skipIndex[0].Offset : 0);
        _input.Seek(scanStart);

        Span<char> charBuf = stackalloc char[256];
        bool foundAny = false;
        while (_input.Position < _dataEnd)
        {
            int termLen = _input.ReadInt32();
            var termSpan = _input.ReadSpan(termLen);
            long postingsOffset = _input.ReadInt64();

            if (termSpan.Length < prefixUtf8Len ||
                !termSpan[..prefixUtf8Len].SequenceEqual(prefixUtf8))
            {
                if (foundAny || termSpan.SequenceCompareTo(prefixUtf8) > 0)
                    break;
                continue;
            }

            foundAny = true;
            var termPartUtf8 = termSpan[prefixUtf8Len..];
            int maxChars = termPartUtf8.Length;
            if (maxChars > charBuf.Length)
                charBuf = new char[maxChars];
            int charCount = System.Text.Encoding.UTF8.GetChars(termPartUtf8, charBuf);

            if (WildcardQuery.Matches(charBuf[..charCount], pattern))
            {
                string qualifiedTerm = System.Text.Encoding.UTF8.GetString(termSpan);
                results.Add((qualifiedTerm, postingsOffset));
            }
        }

        return results;
    }

    /// <summary>
    /// Enumerates terms for a field that are within <paramref name="maxEdits"/> Levenshtein distance
    /// of <paramref name="queryTerm"/>. Uses length-difference pre-filter to skip most terms
    /// without computing edit distance.
    /// </summary>
    public List<(string Term, long Offset)> GetFuzzyMatches(string fieldPrefix, ReadOnlySpan<char> queryTerm, int maxEdits)
    {
        var results = new List<(string, long)>();

        Span<byte> prefixUtf8Buf = fieldPrefix.Length <= 128
            ? stackalloc byte[fieldPrefix.Length * 3]
            : new byte[fieldPrefix.Length * 3];
        int prefixUtf8Len = System.Text.Encoding.UTF8.GetBytes(fieldPrefix.AsSpan(), prefixUtf8Buf);
        var prefixUtf8 = prefixUtf8Buf[..prefixUtf8Len];

        int lo = 0, hi = _skipIndex.Length - 1;
        int bestBlock = -1;
        while (lo <= hi)
        {
            int mid = lo + (hi - lo) / 2;
            int cmp = _skipIndex[mid].Term.AsSpan().SequenceCompareTo(fieldPrefix.AsSpan());
            if (cmp <= 0) { bestBlock = mid; lo = mid + 1; }
            else hi = mid - 1;
        }

        long scanStart = bestBlock >= 0 ? _skipIndex[bestBlock].Offset : (_skipIndex.Length > 0 ? _skipIndex[0].Offset : 0);
        _input.Seek(scanStart);

        int queryTermLen = queryTerm.Length;
        Span<char> charBuf = stackalloc char[256];
        bool foundAny = false;
        while (_input.Position < _dataEnd)
        {
            int termLen = _input.ReadInt32();
            var termSpan = _input.ReadSpan(termLen);
            long postingsOffset = _input.ReadInt64();

            if (termSpan.Length < prefixUtf8Len ||
                !termSpan[..prefixUtf8Len].SequenceEqual(prefixUtf8))
            {
                if (foundAny || termSpan.SequenceCompareTo(prefixUtf8) > 0)
                    break;
                continue;
            }

            foundAny = true;
            var termPartUtf8 = termSpan[prefixUtf8Len..];
            int maxChars = termPartUtf8.Length;

            // Length-difference heuristic: UTF-8 byte count >= char count,
            // so if byte count difference already exceeds maxEdits, skip expensive decode
            if (Math.Abs(maxChars - queryTermLen) > maxEdits)
                continue;

            if (maxChars > charBuf.Length)
                charBuf = new char[maxChars];
            int charCount = System.Text.Encoding.UTF8.GetChars(termPartUtf8, charBuf);

            // Exact char-length check after decode
            if (Math.Abs(charCount - queryTermLen) > maxEdits)
                continue;

            int distance = LevenshteinDistance.Compute(queryTerm, charBuf[..charCount]);
            if (distance <= maxEdits)
            {
                string qualifiedTerm = System.Text.Encoding.UTF8.GetString(termSpan);
                results.Add((qualifiedTerm, postingsOffset));
            }
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

    /// <summary>
    /// Enumerates all terms for a field whose bare term value is within a lexicographic range.
    /// <paramref name="fieldPrefix"/> must be of the form "field\0" (include the null separator).
    /// </summary>
    public List<(string Term, long Offset)> GetTermsInRange(
        string fieldPrefix,
        string? lower, string? upper,
        bool includeLower = true, bool includeUpper = true)
    {
        var results = new List<(string, long)>();

        // Encode field prefix as UTF-8 for byte-level comparison
        Span<byte> prefixUtf8Buf = fieldPrefix.Length <= 128
            ? stackalloc byte[fieldPrefix.Length * 3]
            : new byte[fieldPrefix.Length * 3];
        int prefixUtf8Len = System.Text.Encoding.UTF8.GetBytes(fieldPrefix.AsSpan(), prefixUtf8Buf);
        var prefixUtf8 = prefixUtf8Buf[..prefixUtf8Len];

        // Build qualified lower bound for skip-index entry point search
        string scanKey = lower is not null ? fieldPrefix + lower : fieldPrefix;
        int lo = 0, hi = _skipIndex.Length - 1;
        int bestBlock = -1;
        while (lo <= hi)
        {
            int mid = lo + (hi - lo) / 2;
            int cmp = _skipIndex[mid].Term.AsSpan().SequenceCompareTo(scanKey.AsSpan());
            if (cmp <= 0) { bestBlock = mid; lo = mid + 1; }
            else hi = mid - 1;
        }
        long scanStart = bestBlock >= 0 ? _skipIndex[bestBlock].Offset : (_skipIndex.Length > 0 ? _skipIndex[0].Offset : 0);
        _input.Seek(scanStart);

        Span<char> charBuf = stackalloc char[256];
        while (_input.Position < _dataEnd)
        {
            int termLen = _input.ReadInt32();
            var termSpan = _input.ReadSpan(termLen);
            long postingsOffset = _input.ReadInt64();

            // Must start with fieldPrefix
            if (termSpan.Length < prefixUtf8Len || !termSpan[..prefixUtf8Len].SequenceEqual(prefixUtf8))
            {
                if (termSpan.SequenceCompareTo(prefixUtf8) > 0)
                    break;
                continue;
            }

            // Decode bare term
            var termPartUtf8 = termSpan[prefixUtf8Len..];
            int maxChars = termPartUtf8.Length;
            Span<char> termCharBuf = maxChars <= charBuf.Length ? charBuf[..maxChars] : new char[maxChars];
            int charCount = System.Text.Encoding.UTF8.GetChars(termPartUtf8, termCharBuf);
            var termPart = termCharBuf[..charCount];

            // Lower bound check
            if (lower is not null)
            {
                int cmp = termPart.SequenceCompareTo(lower.AsSpan());
                if (cmp < 0 || (cmp == 0 && !includeLower)) continue;
            }

            // Upper bound check
            if (upper is not null)
            {
                int cmp = termPart.SequenceCompareTo(upper.AsSpan());
                if (cmp > 0 || (cmp == 0 && !includeUpper)) break;
            }

            results.Add((System.Text.Encoding.UTF8.GetString(termSpan), postingsOffset));
        }

        return results;
    }

    /// <summary>
    /// Enumerates all terms for a field whose bare term text matches the given compiled <paramref name="regex"/>.
    /// <paramref name="fieldPrefix"/> must be of the form "field\0" (include the null separator).
    /// </summary>
    public List<(string Term, long Offset)> GetTermsMatchingRegex(string fieldPrefix, Regex regex)
    {
        var results = new List<(string, long)>();

        Span<byte> prefixUtf8Buf = fieldPrefix.Length <= 128
            ? stackalloc byte[fieldPrefix.Length * 3]
            : new byte[fieldPrefix.Length * 3];
        int prefixUtf8Len = System.Text.Encoding.UTF8.GetBytes(fieldPrefix.AsSpan(), prefixUtf8Buf);
        var prefixUtf8 = prefixUtf8Buf[..prefixUtf8Len];

        int lo = 0, hi = _skipIndex.Length - 1;
        int bestBlock = -1;
        while (lo <= hi)
        {
            int mid = lo + (hi - lo) / 2;
            int cmp = _skipIndex[mid].Term.AsSpan().SequenceCompareTo(fieldPrefix.AsSpan());
            if (cmp <= 0) { bestBlock = mid; lo = mid + 1; }
            else hi = mid - 1;
        }
        long scanStart = bestBlock >= 0 ? _skipIndex[bestBlock].Offset : (_skipIndex.Length > 0 ? _skipIndex[0].Offset : 0);
        _input.Seek(scanStart);

        bool foundAny = false;
        Span<char> charBuf = stackalloc char[256];
        while (_input.Position < _dataEnd)
        {
            int termLen = _input.ReadInt32();
            var termSpan = _input.ReadSpan(termLen);
            long postingsOffset = _input.ReadInt64();

            if (termSpan.Length < prefixUtf8Len || !termSpan[..prefixUtf8Len].SequenceEqual(prefixUtf8))
            {
                if (foundAny || termSpan.SequenceCompareTo(prefixUtf8) > 0)
                    break;
                continue;
            }

            foundAny = true;
            var termPartUtf8 = termSpan[prefixUtf8Len..];
            int maxChars = termPartUtf8.Length;
            Span<char> termCharBuf = maxChars <= charBuf.Length ? charBuf[..maxChars] : new char[maxChars];
            int charCount = System.Text.Encoding.UTF8.GetChars(termPartUtf8, termCharBuf);

            if (regex.IsMatch(termCharBuf[..charCount]))
                results.Add((System.Text.Encoding.UTF8.GetString(termSpan), postingsOffset));
        }

        return results;
    }
}
