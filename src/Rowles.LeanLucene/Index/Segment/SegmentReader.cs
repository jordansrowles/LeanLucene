using System.Runtime.CompilerServices;
using Rowles.LeanLucene.Codecs;
using Rowles.LeanLucene.Codecs.DocValues;
using Rowles.LeanLucene.Codecs.StoredFields;
using Rowles.LeanLucene.Store;

namespace Rowles.LeanLucene.Index.Segment;

/// <summary>
/// Reads a single immutable segment from disc via MMapDirectory.
/// </summary>
public sealed partial class SegmentReader : IDisposable
{
    private readonly MMapDirectory _directory;
    private readonly SegmentInfo _info;
    private readonly TermDictionaryReader _dicReader;
    private readonly IndexInput _posInput;
    private readonly byte _postingsVersion;
    private readonly StoredFieldsReader? _storedReader;
    private readonly Dictionary<string, byte[]> _fieldNorms;
    private readonly Dictionary<string, int[]> _fieldLengthsPerField;
    private readonly VectorReader? _vectorReader;
    private readonly Dictionary<(string, string), string> _qualifiedTermCache = new();
    private const int MaxQualifiedTermCacheSize = 8192;
    private LiveDocs? _liveDocs;

    // 64-entry open-addressing term offset cache
    private const int TermCacheSize = 64;
    private const int TermCacheMask = TermCacheSize - 1;
    private readonly string?[] _termCacheKeys = new string?[TermCacheSize];
    private readonly long[] _termCacheOffsets = new long[TermCacheSize];
    private readonly bool[] _termCacheHits = new bool[TermCacheSize];

    // Lazy-loaded Stage 2 features (thread-safe via LazyInitializer)
    private Dictionary<string, Dictionary<int, double>>? _numericIndex;
    private Dictionary<string, double[]>? _numericDocValues;
    private Dictionary<string, string[]>? _sortedDocValues;
    private TermVectorsReader? _termVectorsReader;
    private object? _lazyInitLock;
    private readonly string _basePath;

    public int DocBase { get; set; }

    public SegmentInfo Info => _info;
    public int MaxDoc => _info.DocCount;

    public SegmentReader(MMapDirectory directory, SegmentInfo info)
    {
        _directory = directory;
        _info = info;
        _basePath = Path.Combine(directory.DirectoryPath, info.SegmentId);

        ValidateSegmentFiles(_basePath, info.DocCount);
        _dicReader = TermDictionaryReader.Open(_basePath + ".dic");
        _posInput = directory.OpenInput(info.SegmentId + ".pos");
        _postingsVersion = PostingsEnum.ValidateFileHeader(_posInput);

        var fdtPath = _basePath + ".fdt";
        var fdxPath = _basePath + ".fdx";
        if (File.Exists(fdtPath) && File.Exists(fdxPath))
            _storedReader = StoredFieldsReader.Open(fdtPath, fdxPath);

        var delPath = _basePath + ".del";
        if (File.Exists(delPath))
            _liveDocs = LiveDocs.Deserialise(delPath, info.DocCount);

        // Load per-field norms
        _fieldNorms = NormsReader.Read(_basePath + ".nrm");

        // Prefer exact field lengths from .fln; fall back to quantised norms
        var exactLengths = FieldLengthReader.TryRead(_basePath + ".fln");
        if (exactLengths is not null)
        {
            _fieldLengthsPerField = exactLengths;
        }
        else
        {
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
    /// Returns term vectors for a document, or null if term vectors are not stored for this segment.
    /// Lazily opens the .tvd/.tvx files on first access.
    /// </summary>
    public Dictionary<string, List<TermVectorEntry>>? GetTermVectors(int docId)
    {
        var reader = EnsureTermVectorsReader();
        return reader?.GetTermVector(docId);
    }

    /// <summary>Whether this segment has term vector files.</summary>
    public bool HasTermVectors => File.Exists(_basePath + ".tvd") && File.Exists(_basePath + ".tvx");

    private TermVectorsReader? EnsureTermVectorsReader()
    {
        if (_termVectorsReader is not null) return _termVectorsReader;

        var tvdPath = _basePath + ".tvd";
        var tvxPath = _basePath + ".tvx";
        if (!File.Exists(tvdPath) || !File.Exists(tvxPath)) return null;

        var lockObj = LazyInitializer.EnsureInitialized(ref _lazyInitLock)!;
        lock (lockObj)
        {
            _termVectorsReader ??= TermVectorsReader.Open(tvdPath, tvxPath);
        }
        return _termVectorsReader;
    }

    /// <summary>
    /// Gets or creates a cached qualified term string (field\0term).
    /// </summary>
    private string GetQualifiedTerm(string field, string term)
    {
        var key = (field, term);
        if (!_qualifiedTermCache.TryGetValue(key, out var qt))
        {
            if (_qualifiedTermCache.Count >= MaxQualifiedTermCacheSize)
                _qualifiedTermCache.Clear();
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

    public void Dispose()
    {
        _posInput.Dispose();
        _dicReader.Dispose();
        _storedReader?.Dispose();
        _vectorReader?.Dispose();
        _termVectorsReader?.Dispose();
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
}
