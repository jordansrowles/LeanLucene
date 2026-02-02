using System.Text.RegularExpressions;
using Rowles.LeanLucene.Search;
using Rowles.LeanLucene.Store;

namespace Rowles.LeanLucene.Codecs;

/// <summary>
/// Reads a .dic file produced by <see cref="TermDictionaryWriter"/>.
/// Detects format version automatically:
/// v2 (current): delegates to <see cref="FSTReader"/> for byte-keyed O(log N) lookups.
/// v1 (legacy): materialises all terms into string[] + long[] for backward compatibility.
/// </summary>
public sealed class TermDictionaryReader : IDisposable
{
    // v2 path: delegate to FSTReader
    private readonly FSTReader? _fstReader;

    // v1 fallback: materialised string arrays
    private readonly string[]? _allTerms;
    private readonly long[]? _allOffsets;

    private bool _disposed;

    private TermDictionaryReader(FSTReader fstReader)
    {
        _fstReader = fstReader;
    }

    private TermDictionaryReader(string[] allTerms, long[] allOffsets)
    {
        _allTerms = allTerms;
        _allOffsets = allOffsets;
    }

    public static TermDictionaryReader Open(string filePath)
    {
        using var input = new IndexInput(filePath);

        // Read magic + version
        int magic = input.ReadInt32();
        if (magic != CodecConstants.Magic)
            throw new InvalidDataException(
                $"Invalid term dictionary (.dic) file: expected magic 0x{CodecConstants.Magic:X8}, got 0x{magic:X8}.");
        byte version = input.ReadByte();

        if (version == 2)
        {
            var fst = FSTReader.Open(input);
            return new TermDictionaryReader(fst);
        }

        if (version == 1)
            return OpenV1(input);

        throw new InvalidDataException(
            $"Unsupported term dictionary format version {version}. This build supports up to version {CodecConstants.TermDictionaryVersion}.");
    }

    private static TermDictionaryReader OpenV1(IndexInput input)
    {
        int skipCount = input.ReadInt32();
        for (int i = 0; i < skipCount; i++)
        {
            int termLen = input.ReadInt32();
            input.ReadSpan(termLen);
            input.ReadInt64();
        }

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

    // ── Exact Lookup ────────────────────────────────────────────────────────

    public bool TryGetPostingsOffset(string term, out long offset)
    {
        return TryGetPostingsOffset(term.AsSpan(), out offset);
    }

    public bool TryGetPostingsOffset(ReadOnlySpan<char> term, out long offset)
    {
        if (_fstReader is not null)
            return _fstReader.TryGetPostingsOffset(term, out offset);
        return TryGetPostingsOffsetV1(term, out offset);
    }

    private bool TryGetPostingsOffsetV1(ReadOnlySpan<char> term, out long offset)
    {
        offset = 0;
        int lo = 0, hi = _allTerms!.Length - 1;
        while (lo <= hi)
        {
            int mid = lo + (hi - lo) / 2;
            int cmp = _allTerms[mid].AsSpan().SequenceCompareTo(term);
            if (cmp == 0) { offset = _allOffsets![mid]; return true; }
            if (cmp < 0) lo = mid + 1;
            else hi = mid - 1;
        }
        return false;
    }

    // ── Prefix Scan ─────────────────────────────────────────────────────────

    public List<(string Term, long Offset)> GetTermsWithPrefix(ReadOnlySpan<char> qualifiedPrefix)
    {
        if (_fstReader is not null)
            return _fstReader.GetTermsWithPrefix(qualifiedPrefix);
        return GetTermsWithPrefixV1(qualifiedPrefix);
    }

    private List<(string Term, long Offset)> GetTermsWithPrefixV1(ReadOnlySpan<char> qualifiedPrefix)
    {
        var results = new List<(string, long)>();
        int start = LowerBoundV1(qualifiedPrefix);
        for (int i = start; i < _allTerms!.Length; i++)
        {
            if (_allTerms[i].AsSpan().StartsWith(qualifiedPrefix))
                results.Add((_allTerms[i], _allOffsets![i]));
            else
                break;
        }
        return results;
    }

    // ── Wildcard Scan ───────────────────────────────────────────────────────

    public List<(string Term, long Offset)> GetTermsMatching(string fieldPrefix, ReadOnlySpan<char> pattern)
    {
        if (_fstReader is not null)
            return _fstReader.GetTermsMatching(fieldPrefix, pattern);
        return GetTermsMatchingV1(fieldPrefix, pattern);
    }

    private List<(string Term, long Offset)> GetTermsMatchingV1(string fieldPrefix, ReadOnlySpan<char> pattern)
    {
        var results = new List<(string, long)>();
        int start = LowerBoundV1(fieldPrefix.AsSpan());
        for (int i = start; i < _allTerms!.Length; i++)
        {
            var term = _allTerms[i];
            if (!term.StartsWith(fieldPrefix, StringComparison.Ordinal))
                break;
            var bareTerm = term.AsSpan(fieldPrefix.Length);
            if (WildcardQuery.Matches(bareTerm, pattern))
                results.Add((term, _allOffsets![i]));
        }
        return results;
    }

    // ── Fuzzy Scan ──────────────────────────────────────────────────────────

    public List<(string Term, long Offset)> GetFuzzyMatches(string fieldPrefix, ReadOnlySpan<char> queryTerm, int maxEdits)
    {
        if (_fstReader is not null)
            return _fstReader.GetFuzzyMatches(fieldPrefix, queryTerm, maxEdits);
        return GetFuzzyMatchesV1(fieldPrefix, queryTerm, maxEdits);
    }

    private List<(string Term, long Offset)> GetFuzzyMatchesV1(string fieldPrefix, ReadOnlySpan<char> queryTerm, int maxEdits)
    {
        var results = new List<(string, long)>();
        int start = LowerBoundV1(fieldPrefix.AsSpan());
        int queryTermLen = queryTerm.Length;
        for (int i = start; i < _allTerms!.Length; i++)
        {
            var term = _allTerms[i];
            if (!term.StartsWith(fieldPrefix, StringComparison.Ordinal))
                break;
            var bareTerm = term.AsSpan(fieldPrefix.Length);
            if (Math.Abs(bareTerm.Length - queryTermLen) > maxEdits)
                continue;
            int distance = LevenshteinDistance.Compute(queryTerm, bareTerm);
            if (distance <= maxEdits)
                results.Add((term, _allOffsets![i]));
        }
        return results;
    }

    // ── Field Enumeration ───────────────────────────────────────────────────

    public List<(string Term, long Offset)> GetAllTermsForField(string fieldPrefix)
    {
        return GetTermsWithPrefix(fieldPrefix.AsSpan());
    }

    /// <summary>Enumerates all terms and their postings offsets. Used by SegmentMerger.</summary>
    public List<(string Term, long Offset)> EnumerateAllTerms()
    {
        if (_fstReader is not null)
            return _fstReader.EnumerateAllTerms();

        // v1 fallback
        var results = new List<(string, long)>(_allTerms!.Length);
        for (int i = 0; i < _allTerms.Length; i++)
            results.Add((_allTerms[i], _allOffsets![i]));
        return results;
    }

    // ── Range Scan ──────────────────────────────────────────────────────────

    public List<(string Term, long Offset)> GetTermsInRange(
        string fieldPrefix,
        string? lower, string? upper,
        bool includeLower = true, bool includeUpper = true)
    {
        if (_fstReader is not null)
            return _fstReader.GetTermsInRange(fieldPrefix, lower, upper, includeLower, includeUpper);
        return GetTermsInRangeV1(fieldPrefix, lower, upper, includeLower, includeUpper);
    }

    private List<(string Term, long Offset)> GetTermsInRangeV1(
        string fieldPrefix,
        string? lower, string? upper,
        bool includeLower, bool includeUpper)
    {
        var results = new List<(string, long)>();
        string scanKey = lower is not null ? fieldPrefix + lower : fieldPrefix;
        int start = LowerBoundV1(scanKey.AsSpan());
        for (int i = start; i < _allTerms!.Length; i++)
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
            results.Add((term, _allOffsets![i]));
        }
        return results;
    }

    // ── Regex Scan ──────────────────────────────────────────────────────────

    public List<(string Term, long Offset)> GetTermsMatchingRegex(string fieldPrefix, Regex regex)
    {
        if (_fstReader is not null)
            return _fstReader.GetTermsMatchingRegex(fieldPrefix, regex);
        return GetTermsMatchingRegexV1(fieldPrefix, regex);
    }

    private List<(string Term, long Offset)> GetTermsMatchingRegexV1(string fieldPrefix, Regex regex)
    {
        var results = new List<(string, long)>();
        int start = LowerBoundV1(fieldPrefix.AsSpan());
        for (int i = start; i < _allTerms!.Length; i++)
        {
            var term = _allTerms[i];
            if (!term.StartsWith(fieldPrefix, StringComparison.Ordinal))
                break;
            var bareTerm = term.AsSpan(fieldPrefix.Length);
            if (regex.IsMatch(bareTerm))
                results.Add((term, _allOffsets![i]));
        }
        return results;
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private int LowerBoundV1(ReadOnlySpan<char> key)
    {
        int lo = 0, hi = _allTerms!.Length;
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

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
    }
}
