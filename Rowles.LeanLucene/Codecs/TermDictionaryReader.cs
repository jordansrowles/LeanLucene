using System.Text.RegularExpressions;
using Rowles.LeanLucene.Search;
using Rowles.LeanLucene.Store;

namespace Rowles.LeanLucene.Codecs;

/// <summary>
/// Reads a .dic file produced by <see cref="TermDictionaryWriter"/>.
/// Materialises all terms into sorted in-memory arrays at open time for
/// O(log N) exact lookup and O(log N + K) prefix/range scans with no I/O.
/// </summary>
public sealed class TermDictionaryReader : IDisposable
{
    private readonly string[] _allTerms;
    private readonly long[] _allOffsets;
    private bool _disposed;

    private TermDictionaryReader(string[] allTerms, long[] allOffsets)
    {
        _allTerms = allTerms;
        _allOffsets = allOffsets;
    }

    public static TermDictionaryReader Open(string filePath)
    {
        using var input = new IndexInput(filePath);

        int skipCount = input.ReadInt32();

        // Skip past the skip index entries to reach term data
        for (int i = 0; i < skipCount; i++)
        {
            int termLen = input.ReadInt32();
            input.ReadSpan(termLen);
            input.ReadInt64();
        }

        long dataStart = skipCount > 0
            ? input.Position  // just past skip index — but skip entries point into the data, not here
            : input.Position;

        // The skip index entries' Offset fields point to absolute file positions.
        // The first skip entry's offset is the start of term data.
        // However, after reading skip entries, we're already at the start of term data.
        // Read all term entries from current position to end.
        long dataEnd = input.Length;
        var terms = new List<string>();
        var offsets = new List<long>();

        while (input.Position < dataEnd)
        {
            int termLen = input.ReadInt32();
            string term = System.Text.Encoding.UTF8.GetString(input.ReadSpan(termLen));
            long offset = input.ReadInt64();
            terms.Add(term);
            offsets.Add(offset);
        }

        return new TermDictionaryReader([.. terms], [.. offsets]);
    }

    public bool TryGetPostingsOffset(string term, out long offset)
    {
        return TryGetPostingsOffset(term.AsSpan(), out offset);
    }

    /// <summary>
    /// O(log N) binary search on the in-memory term array — zero I/O, zero heap allocation.
    /// </summary>
    public bool TryGetPostingsOffset(ReadOnlySpan<char> term, out long offset)
    {
        offset = 0;
        int lo = 0, hi = _allTerms.Length - 1;
        while (lo <= hi)
        {
            int mid = lo + (hi - lo) / 2;
            int cmp = _allTerms[mid].AsSpan().SequenceCompareTo(term);
            if (cmp == 0) { offset = _allOffsets[mid]; return true; }
            if (cmp < 0) lo = mid + 1;
            else hi = mid - 1;
        }
        return false;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
    }

    /// <summary>
    /// Enumerates all terms with a given qualified prefix (field\0prefix).
    /// Binary search to first match, then forward scan in sorted memory.
    /// </summary>
    public List<(string Term, long Offset)> GetTermsWithPrefix(ReadOnlySpan<char> qualifiedPrefix)
    {
        var results = new List<(string, long)>();
        int start = LowerBound(qualifiedPrefix);

        for (int i = start; i < _allTerms.Length; i++)
        {
            if (_allTerms[i].AsSpan().StartsWith(qualifiedPrefix))
                results.Add((_allTerms[i], _allOffsets[i]));
            else if (i > start || _allTerms[i].AsSpan().SequenceCompareTo(qualifiedPrefix) > 0)
                break;
        }

        return results;
    }

    /// <summary>
    /// Enumerates all terms matching a wildcard pattern for a given field.
    /// </summary>
    public List<(string Term, long Offset)> GetTermsMatching(string fieldPrefix, ReadOnlySpan<char> pattern)
    {
        var results = new List<(string, long)>();
        int start = LowerBound(fieldPrefix.AsSpan());

        for (int i = start; i < _allTerms.Length; i++)
        {
            var term = _allTerms[i];
            if (!term.StartsWith(fieldPrefix, StringComparison.Ordinal))
                break;

            var bareTerm = term.AsSpan(fieldPrefix.Length);
            if (WildcardQuery.Matches(bareTerm, pattern))
                results.Add((term, _allOffsets[i]));
        }

        return results;
    }

    /// <summary>
    /// Enumerates terms for a field within Levenshtein distance.
    /// </summary>
    public List<(string Term, long Offset)> GetFuzzyMatches(string fieldPrefix, ReadOnlySpan<char> queryTerm, int maxEdits)
    {
        var results = new List<(string, long)>();
        int start = LowerBound(fieldPrefix.AsSpan());
        int queryTermLen = queryTerm.Length;

        for (int i = start; i < _allTerms.Length; i++)
        {
            var term = _allTerms[i];
            if (!term.StartsWith(fieldPrefix, StringComparison.Ordinal))
                break;

            var bareTerm = term.AsSpan(fieldPrefix.Length);

            // Length-difference pre-filter
            if (Math.Abs(bareTerm.Length - queryTermLen) > maxEdits)
                continue;

            int distance = LevenshteinDistance.Compute(queryTerm, bareTerm);
            if (distance <= maxEdits)
                results.Add((term, _allOffsets[i]));
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
    /// </summary>
    public List<(string Term, long Offset)> GetTermsInRange(
        string fieldPrefix,
        string? lower, string? upper,
        bool includeLower = true, bool includeUpper = true)
    {
        var results = new List<(string, long)>();

        // Start scan from qualified lower bound if available
        string scanKey = lower is not null ? fieldPrefix + lower : fieldPrefix;
        int start = LowerBound(scanKey.AsSpan());

        for (int i = start; i < _allTerms.Length; i++)
        {
            var term = _allTerms[i];
            if (!term.StartsWith(fieldPrefix, StringComparison.Ordinal))
                break;

            var bareTerm = term.AsSpan(fieldPrefix.Length);

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

            results.Add((term, _allOffsets[i]));
        }

        return results;
    }

    /// <summary>
    /// Enumerates all terms for a field whose bare term text matches the given compiled regex.
    /// </summary>
    public List<(string Term, long Offset)> GetTermsMatchingRegex(string fieldPrefix, Regex regex)
    {
        var results = new List<(string, long)>();
        int start = LowerBound(fieldPrefix.AsSpan());

        for (int i = start; i < _allTerms.Length; i++)
        {
            var term = _allTerms[i];
            if (!term.StartsWith(fieldPrefix, StringComparison.Ordinal))
                break;

            var bareTerm = term.AsSpan(fieldPrefix.Length);
            if (regex.IsMatch(bareTerm))
                results.Add((term, _allOffsets[i]));
        }

        return results;
    }

    /// <summary>
    /// Returns the first index where _allTerms[i] >= key (lower bound).
    /// </summary>
    private int LowerBound(ReadOnlySpan<char> key)
    {
        int lo = 0, hi = _allTerms.Length;
        while (lo < hi)
        {
            int mid = lo + (hi - lo) / 2;
            if (_allTerms[mid].AsSpan().SequenceCompareTo(key) < 0)
                lo = mid + 1;
            else
                hi = mid;
        }
        return lo;
    }
}
