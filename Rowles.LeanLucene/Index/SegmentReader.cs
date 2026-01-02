using System.Runtime.CompilerServices;
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
    private readonly float[] _norms;
    private readonly int[] _fieldLengths;
    private readonly Dictionary<string, Dictionary<int, double>> _numericIndex = new();
    private readonly VectorReader? _vectorReader;
    private readonly Dictionary<string, double[]> _numericDocValues;
    private readonly Dictionary<string, string[]> _sortedDocValues;
    private LiveDocs? _liveDocs;
    private string? _lastQualifiedTerm;
    private long _lastPostingsOffset;
    private bool _lastLookupHit;

    public int DocBase { get; set; }

    public SegmentInfo Info => _info;
    public int MaxDoc => _info.DocCount;

    public SegmentReader(MMapDirectory directory, SegmentInfo info)
    {
        _directory = directory;
        _info = info;

        var basePath = Path.Combine(directory.DirectoryPath, info.SegmentId);
        ValidateSegmentFiles(basePath, info.DocCount);
        _dicReader = TermDictionaryReader.Open(basePath + ".dic");
        _posInput = directory.OpenInput(info.SegmentId + ".pos");

        var fdtPath = basePath + ".fdt";
        var fdxPath = basePath + ".fdx";
        if (File.Exists(fdtPath) && File.Exists(fdxPath))
            _storedReader = StoredFieldsReader.Open(fdtPath, fdxPath);

        var delPath = basePath + ".del";
        if (File.Exists(delPath))
            _liveDocs = LiveDocs.Deserialise(delPath, info.DocCount);

        _norms = NormsReader.Read(basePath + ".nrm");

        // Precompute field lengths from norms to avoid per-doc float division in scoring
        _fieldLengths = new int[_norms.Length];
        for (int i = 0; i < _norms.Length; i++)
        {
            float n = _norms[i];
            _fieldLengths[i] = n <= 0f ? 1 : Math.Max(1, (int)MathF.Round(1.0f / n - 1.0f));
        }

        var numPath = basePath + ".num";
        if (File.Exists(numPath))
            _numericIndex = ReadNumericIndex(numPath);

        var vecPath = basePath + ".vec";
        if (File.Exists(vecPath))
            _vectorReader = VectorReader.Open(vecPath);

        _numericDocValues = NumericDocValuesReader.Read(basePath + ".dvn");
        _sortedDocValues = SortedDocValuesReader.Read(basePath + ".dvs");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsLive(int docId) => _liveDocs?.IsLive(docId) ?? true;

    /// <summary>True when this segment has no deleted documents, allowing callers to skip per-doc IsLive checks.</summary>
    public bool HasDeletions => _liveDocs is not null;

    /// <summary>Returns the quantised norm value for a document (0..1 range).</summary>
    public float GetNorm(int docId)
    {
        if (docId < 0 || docId >= _norms.Length)
            return 0f;
        return _norms[docId];
    }

    /// <summary>
    /// Returns an approximate field length for BM25, derived from the stored norm.
    /// Norm was stored as 1/(1+tokenCount), so fieldLength ≈ (1/norm) - 1.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetFieldLength(int docId)
    {
        return (uint)docId < (uint)_fieldLengths.Length ? _fieldLengths[docId] : 1;
    }

    /// <summary>
    /// Returns document IDs matching the given field and term.
    /// </summary>
    public int[] GetDocIds(string field, string term)
    {
        var qualifiedTerm = $"{field}\x00{term}";
        if (!_dicReader.TryGetPostingsOffset(qualifiedTerm, out long offset))
            return [];

        return ReadPostingsAtOffset(offset);
    }

    /// <summary>
    /// Returns the term frequency for a given term in a specific document.
    /// </summary>
    public int GetTermFrequency(string field, string term, int docId)
    {
        var qualifiedTerm = $"{field}\x00{term}";
        if (!_dicReader.TryGetPostingsOffset(qualifiedTerm, out long offset))
            return 0;

        return ReadTermFrequency(offset, docId);
    }

    /// <summary>Returns the document frequency for a term (count only, no full decode).</summary>
    public int GetDocFreq(string field, string term)
    {
        var qualifiedTerm = $"{field}\x00{term}";
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

    /// <summary>Single-entry cache for the most recent term lookup.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool TryGetCachedOffset(string qualifiedTerm, out long offset)
    {
        if (ReferenceEquals(qualifiedTerm, _lastQualifiedTerm))
        {
            offset = _lastPostingsOffset;
            return _lastLookupHit;
        }

        bool found = _dicReader.TryGetPostingsOffset(qualifiedTerm, out offset);
        _lastQualifiedTerm = qualifiedTerm;
        _lastPostingsOffset = offset;
        _lastLookupHit = found;
        return found;
    }

    /// <summary>Returns positional data for a term in a specific document, or null if unavailable.</summary>
    public int[]? GetPositions(string field, string term, int docId)
    {
        var qualifiedTerm = $"{field}\x00{term}";
        if (!_dicReader.TryGetPostingsOffset(qualifiedTerm, out long offset))
            return null;

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

    public Dictionary<string, string> GetStoredFields(int docId)
    {
        return _storedReader?.ReadDocument(docId) ?? new Dictionary<string, string>();
    }

    /// <summary>
    /// Tries to get a numeric field value for a document from the .num index.
    /// </summary>
    public bool TryGetNumericValue(string field, int docId, out double value)
    {
        value = 0;
        // Prefer column-stride DocValues (faster, no dict lookup per doc)
        if (_numericDocValues.TryGetValue(field, out var dvArr) && (uint)docId < (uint)dvArr.Length)
        {
            value = dvArr[docId];
            return true;
        }
        return _numericIndex.TryGetValue(field, out var fieldMap) && fieldMap.TryGetValue(docId, out value);
    }

    /// <summary>
    /// Tries to get a string DocValues field value for a document.
    /// </summary>
    public bool TryGetSortedDocValue(string field, int docId, out string value)
    {
        value = string.Empty;
        if (_sortedDocValues.TryGetValue(field, out var arr) && (uint)docId < (uint)arr.Length)
        {
            value = arr[docId];
            return true;
        }
        return false;
    }

    /// <summary>Returns the NumericDocValues array for a field, or null if unavailable.</summary>
    public double[]? GetNumericDocValues(string field)
        => _numericDocValues.GetValueOrDefault(field);

    /// <summary>Returns the SortedDocValues array for a field, or null if unavailable.</summary>
    public string[]? GetSortedDocValues(string field)
        => _sortedDocValues.GetValueOrDefault(field);

    /// <summary>
    /// Returns all document IDs that have a numeric value in the given field within the specified range.
    /// </summary>
    public List<(int DocId, double Value)> GetNumericRange(string field, double min, double max)
    {
        var results = new List<(int, double)>();
        if (!_numericIndex.TryGetValue(field, out var fieldMap))
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
        if (nrmLength < docCount)
            throw new InvalidDataException(
                $"Segment norms file '{basePath}.nrm' is truncated: expected at least {docCount} bytes, found {nrmLength}.");
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
