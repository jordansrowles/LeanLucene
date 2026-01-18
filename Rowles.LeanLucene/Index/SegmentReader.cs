using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Rowles.LeanLucene.Codecs;
using Rowles.LeanLucene.Store;

namespace Rowles.LeanLucene.Index;

/// <summary>
/// Reads a single immutable segment from disc via MMapDirectory.
/// </summary>
public sealed class SegmentReader : IDisposable
{
    private readonly MMapDirectory _directory;
    private readonly SegmentInfo _info;
    private readonly TermDictionaryReader _dicReader;
    private readonly IndexInput _posInput;
    private readonly StoredFieldsReader? _storedReader;
    private readonly Dictionary<string, byte[]> _fieldNorms;
    private readonly Dictionary<string, int[]> _fieldLengthsPerField;
    private readonly VectorReader? _vectorReader;
    private readonly Dictionary<(string, string), string> _qualifiedTermCache = new();
    private LiveDocs? _liveDocs;

    // 16-entry open-addressing term offset cache (replaces single-entry cache)
    private const int TermCacheSize = 16;
    private const int TermCacheMask = TermCacheSize - 1;
    private readonly string?[] _termCacheKeys = new string?[TermCacheSize];
    private readonly long[] _termCacheOffsets = new long[TermCacheSize];
    private readonly bool[] _termCacheHits = new bool[TermCacheSize];

    // Lazy-loaded Stage 2 features to avoid startup regression
    private Dictionary<string, Dictionary<int, double>>? _numericIndex;
    private bool _numericIndexInitialized;
    private Dictionary<string, double[]>? _numericDocValues;
    private bool _numericDocValuesInitialized;
    private Dictionary<string, string[]>? _sortedDocValues;
    private bool _sortedDocValuesInitialized;
    private readonly string _basePath;

    public int DocBase { get; set; }

    public SegmentInfo Info => _info;
    public int MaxDoc => _info.DocCount;

    /// <summary>Lazy-loads the numeric index (.num) for range queries.</summary>
    private Dictionary<string, Dictionary<int, double>> EnsureNumericIndex()
    {
        if (!_numericIndexInitialized)
        {
            _numericIndexInitialized = true;
            var numPath = _basePath + ".num";
            if (File.Exists(numPath))
                _numericIndex = ReadNumericIndex(numPath);
            else
                _numericIndex = new Dictionary<string, Dictionary<int, double>>();
        }
        return _numericIndex!;
    }

    /// <summary>Lazy-loads numeric doc values (.dvn) for per-document numeric retrieval.</summary>
    private Dictionary<string, double[]> EnsureNumericDocValues()
    {
        if (!_numericDocValuesInitialized)
        {
            _numericDocValuesInitialized = true;
            _numericDocValues = NumericDocValuesReader.Read(_basePath + ".dvn");
        }
        return _numericDocValues!;
    }

    /// <summary>Lazy-loads sorted doc values (.dvs) for per-document string retrieval.</summary>
    private Dictionary<string, string[]> EnsureSortedDocValues()
    {
        if (!_sortedDocValuesInitialized)
        {
            _sortedDocValuesInitialized = true;
            _sortedDocValues = SortedDocValuesReader.Read(_basePath + ".dvs");
        }
        return _sortedDocValues!;
    }

    public SegmentReader(MMapDirectory directory, SegmentInfo info)
    {
        _directory = directory;
        _info = info;
        _basePath = Path.Combine(directory.DirectoryPath, info.SegmentId);

        ValidateSegmentFiles(_basePath, info.DocCount);
        _dicReader = TermDictionaryReader.Open(_basePath + ".dic");
        _posInput = directory.OpenInput(info.SegmentId + ".pos");

        var fdtPath = _basePath + ".fdt";
        var fdxPath = _basePath + ".fdx";
        if (File.Exists(fdtPath) && File.Exists(fdxPath))
            _storedReader = StoredFieldsReader.Open(fdtPath, fdxPath);

        var delPath = _basePath + ".del";
        if (File.Exists(delPath))
            _liveDocs = LiveDocs.Deserialise(delPath, info.DocCount);

        // Load per-field norms
        _fieldNorms = NormsReader.Read(_basePath + ".nrm");

        // Precompute per-field lengths from norms to avoid per-doc float division in scoring
        _fieldLengthsPerField = new Dictionary<string, int[]>(_fieldNorms.Count, StringComparer.Ordinal);
        foreach (var (fieldName, norms) in _fieldNorms)
        {
            var fieldLengths = new int[norms.Length];
            for (int i = 0; i < norms.Length; i++)
            {
                float n = norms[i] / 255f;
                fieldLengths[i] = n <= 0f ? 1 : Math.Max(1, (int)MathF.Round(1.0f / n - 1.0f));
            }
            _fieldLengthsPerField[fieldName] = fieldLengths;
        }

        var vecPath = _basePath + ".vec";
        if (File.Exists(vecPath))
            _vectorReader = VectorReader.Open(vecPath);

        // Stage 2 features: numeric index, numeric doc values, and sorted doc values are now lazy-loaded
        // to avoid startup regression for simple TermQuery and BooleanQuery operations
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsLive(int docId) => _liveDocs?.IsLive(docId) ?? true;

    /// <summary>True when this segment has no deleted documents, allowing callers to skip per-doc IsLive checks.</summary>
    public bool HasDeletions => _liveDocs is not null;

    /// <summary>Returns the quantised norm value for a document in a specific field (0..1 range).</summary>
    public float GetNorm(int docId, string field)
    {
        if (_fieldNorms.TryGetValue(field, out var norms) && (uint)docId < (uint)norms.Length)
            return norms[docId] / 255f;
        return 0f;
    }

    /// <summary>Returns the quantised norm value for a document using the first available field.</summary>
    public float GetNorm(int docId)
    {
        foreach (var norms in _fieldNorms.Values)
        {
            if ((uint)docId < (uint)norms.Length)
                return norms[docId] / 255f;
        }
        return 0f;
    }

    /// <summary>
    /// Returns an approximate field length for BM25 for a specific field, derived from the stored norm.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetFieldLength(int docId, string field)
    {
        if (_fieldLengthsPerField.TryGetValue(field, out var fieldLengths))
            return (uint)docId < (uint)fieldLengths.Length ? fieldLengths[docId] : 1;
        return 1;
    }

    /// <summary>
    /// Returns an approximate field length using the first available field (for non-field-specific queries).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetFieldLength(int docId)
    {
        foreach (var fieldLengths in _fieldLengthsPerField.Values)
        {
            if ((uint)docId < (uint)fieldLengths.Length)
                return fieldLengths[docId];
        }
        return 1;
    }

    /// <summary>
    /// Gets or creates a cached qualified term string (field\0term).
    /// </summary>
    private string GetQualifiedTerm(string field, string term)
    {
        var key = (field, term);
        if (!_qualifiedTermCache.TryGetValue(key, out var qt))
        {
            qt = string.Concat(field, "\x00", term);
            _qualifiedTermCache[key] = qt;
        }
        return qt;
    }

    /// <summary>
    /// Returns document IDs matching the given field and term.
    /// </summary>
    public int[] GetDocIds(string field, string term)
    {
        var qualifiedTerm = GetQualifiedTerm(field, term);
        if (!_dicReader.TryGetPostingsOffset(qualifiedTerm, out long offset))
            return [];

        return ReadPostingsAtOffset(offset);
    }

    /// <summary>
    /// Returns document IDs for a pre-built qualified term string.
    /// </summary>
    internal int[] GetDocIds(string qualifiedTerm)
    {
        if (!_dicReader.TryGetPostingsOffset(qualifiedTerm, out long offset))
            return [];

        return ReadPostingsAtOffset(offset);
    }

    /// <summary>
    /// Returns the term frequency for a given term in a specific document.
    /// </summary>
    public int GetTermFrequency(string field, string term, int docId)
    {
        var qualifiedTerm = GetQualifiedTerm(field, term);
        if (!_dicReader.TryGetPostingsOffset(qualifiedTerm, out long offset))
            return 0;

        return ReadTermFrequency(offset, docId);
    }

    /// <summary>
    /// Returns the term frequency for a pre-built qualified term string.
    /// </summary>
    internal int GetTermFrequency(string qualifiedTerm, int docId)
    {
        if (!_dicReader.TryGetPostingsOffset(qualifiedTerm, out long offset))
            return 0;

        return ReadTermFrequency(offset, docId);
    }

    /// <summary>Returns the document frequency for a term (count only, no full decode).</summary>
    public int GetDocFreq(string field, string term)
    {
        var qualifiedTerm = GetQualifiedTerm(field, term);
        return GetDocFreqByQualified(qualifiedTerm);
    }

    /// <summary>Returns the document frequency for a pre-built qualified term string.</summary>
    internal int GetDocFreq(string qualifiedTerm)
    {
        return GetDocFreqByQualified(qualifiedTerm);
    }

    /// <summary>Returns the document frequency using a pre-built qualified term string.</summary>
    public int GetDocFreqByQualified(string qualifiedTerm)
    {
        if (!TryGetCachedOffset(qualifiedTerm, out long offset))
            return 0;

        _posInput.Seek(offset);
        return _posInput.ReadInt32();
    }

    /// <summary>
    /// Returns a PostingsEnum cursor for the given qualified term (field\0term).
    /// Decodes the postings list once; caller must dispose.
    /// </summary>
    public PostingsEnum GetPostingsEnum(string qualifiedTerm)
    {
        if (!TryGetCachedOffset(qualifiedTerm, out long offset))
            return PostingsEnum.Empty;

        return PostingsEnum.Create(_posInput, offset);
    }

    /// <summary>
    /// Returns a PostingsEnum at a known postings offset, skipping the dictionary lookup.
    /// Use when the offset was already obtained from a term scan (e.g. prefix/wildcard).
    /// </summary>
    public PostingsEnum GetPostingsEnumAtOffset(long offset)
        => PostingsEnum.Create(_posInput, offset);

    /// <summary>
    /// Returns a PostingsEnum with decoded positions for phrase queries.
    /// </summary>
    public PostingsEnum GetPostingsEnumWithPositions(string qualifiedTerm)
    {
        if (!TryGetCachedOffset(qualifiedTerm, out long offset))
            return PostingsEnum.Empty;

        return PostingsEnum.CreateWithPositions(_posInput, offset);
    }

    /// <summary>16-entry open-addressing cache for recent term lookups.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool TryGetCachedOffset(string qualifiedTerm, out long offset)
    {
        int slot = qualifiedTerm.GetHashCode() & TermCacheMask;
        if (ReferenceEquals(qualifiedTerm, _termCacheKeys[slot]))
        {
            offset = _termCacheOffsets[slot];
            return _termCacheHits[slot];
        }

        bool found = _dicReader.TryGetPostingsOffset(qualifiedTerm, out offset);
        _termCacheKeys[slot] = qualifiedTerm;
        _termCacheOffsets[slot] = offset;
        _termCacheHits[slot] = found;
        return found;
    }

    /// <summary>Returns positional data for a term in a specific document, or null if unavailable.</summary>
    public int[]? GetPositions(string field, string term, int docId)
    {
        var qualifiedTerm = GetQualifiedTerm(field, term);
        if (!_dicReader.TryGetPostingsOffset(qualifiedTerm, out long offset))
            return null;

        return ReadPositionsAtOffset(offset, docId);
    }

    /// <summary>Returns positional data for a pre-built qualified term string.</summary>
    internal ReadOnlySpan<int> GetPositions(string qualifiedTerm, int docId)
    {
        if (!_dicReader.TryGetPostingsOffset(qualifiedTerm, out long offset))
            return ReadOnlySpan<int>.Empty;

        var positions = ReadPositionsAtOffset(offset, docId);
        return positions.AsSpan();
    }

    private int[]? ReadPositionsAtOffset(long offset, int docId)
    {
        _posInput.Seek(offset);
        int count = _posInput.ReadInt32();
        // Skip past skip entries
        int skipCount = _posInput.ReadInt32();
        if (skipCount > 0)
            _posInput.Seek(_posInput.Position + skipCount * 8L);

        // Stream through doc IDs to find target index (zero alloc)
        int targetIndex = -1;
        int prev = 0;
        for (int i = 0; i < count; i++)
        {
            prev = ReadNextDocId(_posInput, prev);
            if (prev == docId && targetIndex < 0)
                targetIndex = i;
        }
        if (targetIndex < 0) return null;

        bool hasFreqs = _posInput.ReadBoolean();
        if (hasFreqs)
        {
            for (int i = 0; i < count; i++)
                _posInput.ReadVarInt(); // skip freqs
        }

        bool hasPositions = _posInput.ReadBoolean();
        if (!hasPositions) return null;

        for (int i = 0; i < targetIndex; i++)
        {
            int posCount = _posInput.ReadVarInt();
            for (int j = 0; j < posCount; j++)
                _posInput.ReadVarInt();
        }

        int targetPosCount = _posInput.ReadVarInt();
        var positions = new int[targetPosCount];
        int prevPos = 0;
        for (int i = 0; i < targetPosCount; i++)
        {
            prevPos += _posInput.ReadVarInt();
            positions[i] = prevPos;
        }

        return positions;
    }

    public IReadOnlyDictionary<string, IReadOnlyList<string>> GetStoredFields(int docId)
    {
        if (_storedReader is null)
            return new Dictionary<string, IReadOnlyList<string>>();
        
        var raw = _storedReader.ReadDocument(docId);
        // Convert to read-only types
        return raw.ToDictionary(
            kvp => kvp.Key, 
            kvp => (IReadOnlyList<string>)kvp.Value.AsReadOnly());
    }

    /// <summary>
    /// Tries to get a numeric field value for a document from the .num index.
    /// </summary>
    public bool TryGetNumericValue(string field, int docId, out double value)
    {
        value = 0;
        // Prefer column-stride DocValues (faster, no dict lookup per doc)
        var numericDocValues = EnsureNumericDocValues();
        if (numericDocValues.TryGetValue(field, out var dvArr) && (uint)docId < (uint)dvArr.Length)
        {
            value = dvArr[docId];
            return true;
        }
        var numericIndex = EnsureNumericIndex();
        return numericIndex.TryGetValue(field, out var fieldMap) && fieldMap.TryGetValue(docId, out value);
    }

    /// <summary>
    /// Tries to get a string DocValues field value for a document.
    /// </summary>
    public bool TryGetSortedDocValue(string field, int docId, out string value)
    {
        value = string.Empty;
        var sortedDocValues = EnsureSortedDocValues();
        if (sortedDocValues.TryGetValue(field, out var arr) && (uint)docId < (uint)arr.Length)
        {
            value = arr[docId];
            return true;
        }
        return false;
    }

    /// <summary>Returns the NumericDocValues array for a field, or null if unavailable.</summary>
    public double[]? GetNumericDocValues(string field)
        => EnsureNumericDocValues().GetValueOrDefault(field);

    /// <summary>Returns the SortedDocValues array for a field, or null if unavailable.</summary>
    public string[]? GetSortedDocValues(string field)
        => EnsureSortedDocValues().GetValueOrDefault(field);

    /// <summary>
    /// Returns all document IDs that have a numeric value in the given field within the specified range.
    /// </summary>
    public List<(int DocId, double Value)> GetNumericRange(string field, double min, double max)
    {
        var results = new List<(int, double)>();
        var numericIndex = EnsureNumericIndex();
        if (!numericIndex.TryGetValue(field, out var fieldMap))
            return results;

        foreach (var (docId, value) in fieldMap)
        {
            if (value >= min && value <= max && IsLive(docId))
                results.Add((docId, value));
        }
        return results;
    }

    /// <summary>Returns whether this segment has vector data.</summary>
    public bool HasVectors => _vectorReader is not null;

    /// <summary>Reads the vector for a given document.</summary>
    public float[]? GetVector(int docId)
    {
        return _vectorReader?.ReadVector(docId);
    }

    /// <summary>Returns all terms matching a qualified prefix.</summary>
    public List<(string Term, long Offset)> GetTermsWithPrefix(string qualifiedPrefix)
    {
        return _dicReader.GetTermsWithPrefix(qualifiedPrefix.AsSpan());
    }

    /// <summary>Returns all terms for a field matching a wildcard pattern.</summary>
    public List<(string Term, long Offset)> GetTermsMatching(string fieldPrefix, ReadOnlySpan<char> pattern)
    {
        return _dicReader.GetTermsMatching(fieldPrefix, pattern);
    }

    /// <summary>Returns all terms for a given field.</summary>
    public List<(string Term, long Offset)> GetAllTermsForField(string fieldPrefix)
    {
        return _dicReader.GetAllTermsForField(fieldPrefix);
    }

    /// <summary>Returns terms within Levenshtein distance of queryTerm.</summary>
    public List<(string Term, long Offset)> GetFuzzyMatches(string fieldPrefix, ReadOnlySpan<char> queryTerm, int maxEdits)
    {
        return _dicReader.GetFuzzyMatches(fieldPrefix, queryTerm, maxEdits);
    }

    /// <summary>Returns terms in lexicographic range [lower, upper] for a field.</summary>
    public List<(string Term, long Offset)> GetTermsInRange(string fieldPrefix,
        string? lower, string? upper, bool includeLower = true, bool includeUpper = true)
    {
        return _dicReader.GetTermsInRange(fieldPrefix, lower, upper, includeLower, includeUpper);
    }

    /// <summary>Returns terms for a field matching the compiled regex.</summary>
    public List<(string Term, long Offset)> GetTermsMatchingRegex(string fieldPrefix, Regex regex)
    {
        return _dicReader.GetTermsMatchingRegex(fieldPrefix, regex);
    }

    private int[] ReadPostingsAtOffset(long offset)
    {
        _posInput.Seek(offset);
        int count = _posInput.ReadInt32();
        // Skip past skip entries
        int skipCount = _posInput.ReadInt32();
        if (skipCount > 0)
            _posInput.Seek(_posInput.Position + skipCount * 8L);
        var ids = new int[count];
        int prev = 0;
        for (int i = 0; i < count; i++)
        {
            prev = ReadNextDocId(_posInput, prev);
            ids[i] = prev;
        }
        return ids;
    }

    private int ReadTermFrequency(long offset, int targetDocId)
    {
        _posInput.Seek(offset);
        int count = _posInput.ReadInt32();
        // Skip past skip entries
        int skipCount = _posInput.ReadInt32();
        if (skipCount > 0)
            _posInput.Seek(_posInput.Position + skipCount * 8L);

        var ids = new int[count];
        int prev = 0;
        for (int i = 0; i < count; i++)
        {
            prev = ReadNextDocId(_posInput, prev);
            ids[i] = prev;
        }

        bool hasFreqs = _posInput.ReadBoolean();
        if (!hasFreqs) return 1;

        for (int i = 0; i < count; i++)
        {
            int freq = _posInput.ReadVarInt();
            if (ids[i] == targetDocId)
                return freq;
        }

        return 0;
    }

    public void Dispose()
    {
        _posInput.Dispose();
        _dicReader.Dispose();
        _storedReader?.Dispose();
        _vectorReader?.Dispose();
    }

    private static int ReadNextDocId(IndexInput input, int previous)
    {
        int delta = input.ReadVarInt();
        if (delta < 0)
            throw new InvalidDataException("Postings data is corrupt: negative delta encountered.");

        try
        {
            return checked(previous + delta);
        }
        catch (OverflowException ex)
        {
            throw new InvalidDataException("Postings data is corrupt: doc ID delta overflow.", ex);
        }
    }

    private static void ValidateSegmentFiles(string basePath, int docCount)
    {
        ValidateExistingFile(basePath + ".seg");
        ValidateExistingFile(basePath + ".dic");
        ValidateExistingFile(basePath + ".pos");
        ValidateExistingFile(basePath + ".nrm");

        var segLength = new FileInfo(basePath + ".seg").Length;
        if (segLength == 0)
            throw new InvalidDataException($"Segment metadata file is empty or truncated: '{basePath}.seg'.");

        var dicLength = new FileInfo(basePath + ".dic").Length;
        if (dicLength < sizeof(int))
            throw new InvalidDataException($"Segment dictionary file is truncated: '{basePath}.dic'.");

        var nrmLength = new FileInfo(basePath + ".nrm").Length;
        // Per-field format: 4-byte field count header = minimum 4 bytes
        if (nrmLength < 4)
            throw new InvalidDataException(
                $"Segment norms file '{basePath}.nrm' is truncated: expected at least 4 bytes, found {nrmLength}.");
    }

    private static void ValidateExistingFile(string path)
    {
        var info = new FileInfo(path);
        if (!info.Exists)
            throw new FileNotFoundException($"Segment file is missing: '{path}'.", path);
    }

    private static Dictionary<string, Dictionary<int, double>> ReadNumericIndex(string filePath)
    {
        var result = new Dictionary<string, Dictionary<int, double>>();
        using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var reader = new BinaryReader(fs, System.Text.Encoding.UTF8, leaveOpen: false);

        int fieldCount = reader.ReadInt32();
        for (int f = 0; f < fieldCount; f++)
        {
            string fieldName = reader.ReadString();
            int entryCount = reader.ReadInt32();
            var fieldMap = new Dictionary<int, double>(entryCount);
            for (int e = 0; e < entryCount; e++)
            {
                int docId = reader.ReadInt32();
                double value = reader.ReadDouble();
                fieldMap[docId] = value;
            }
            result[fieldName] = fieldMap;
        }
        return result;
    }
}
