using System.Runtime.CompilerServices;
using Rowles.LeanLucene.Codecs;
using Rowles.LeanLucene.Codecs.Hnsw;
using Rowles.LeanLucene.Codecs.Fst;
using Rowles.LeanLucene.Codecs.Bkd;
using Rowles.LeanLucene.Codecs.Vectors;
using Rowles.LeanLucene.Codecs.TermVectors;
using Rowles.LeanLucene.Codecs.TermDictionary;
using Rowles.LeanLucene.Codecs.DocValues;

namespace Rowles.LeanLucene.Index.Segment;

/// <summary>
/// DocValues and numeric index-related methods for SegmentReader.
/// </summary>
public sealed partial class SegmentReader
{
    /// <summary>Lazy-loads the numeric index (.num) for range queries.</summary>
    private Dictionary<string, Dictionary<int, double>> EnsureNumericIndex()
    {
        return LazyInitializer.EnsureInitialized(ref _numericIndex, ref _lazyInitLock, () =>
        {
            var numPath = _basePath + ".num";
            return File.Exists(numPath)
                ? ReadNumericIndex(numPath)
                : new Dictionary<string, Dictionary<int, double>>();
        })!;
    }

    /// <summary>Lazy-loads numeric doc values (.dvn) for per-document numeric retrieval.</summary>
    private Dictionary<string, double[]> EnsureNumericDocValues()
    {
        return LazyInitializer.EnsureInitialized(ref _numericDocValues, ref _lazyInitLock,
            () => NumericDocValuesReader.Read(_basePath + ".dvn"))!;
    }

    /// <summary>Lazy-loads sorted doc values (.dvs) for per-document string retrieval.</summary>
    private Dictionary<string, string[]> EnsureSortedDocValues()
    {
        return LazyInitializer.EnsureInitialized(ref _sortedDocValues, ref _lazyInitLock,
            () => SortedDocValuesReader.Read(_basePath + ".dvs"))!;
    }

    /// <summary>
    /// Tries to get a numeric field value for a document from the .num index.
    /// </summary>
    public bool TryGetNumericValue(string field, int docId, out double value)
    {
        value = 0;
        var numericIndex = EnsureNumericIndex();
        if (numericIndex.TryGetValue(field, out var fieldMap))
            return fieldMap.TryGetValue(docId, out value);

        // Legacy fallback for segments that predate the sparse .num index.
        var numericDocValues = EnsureNumericDocValues();
        if (numericDocValues.TryGetValue(field, out var dvArr) && (uint)docId < (uint)dvArr.Length)
        {
            value = dvArr[docId];
            return true;
        }

        return false;
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
    public bool HasVectors => _vectorReaders.Count > 0;

    /// <summary>Reads the vector for a given document from the first available vector field (legacy convenience).</summary>
    public float[]? GetVector(int docId)
    {
        foreach (var kv in _vectorReaders)
            return kv.Value.ReadVector(docId);
        return null;
    }

    /// <summary>Reads the vector for a given document on the named vector field.</summary>
    public float[]? GetVector(string fieldName, int docId)
    {
        if (_vectorReaders.TryGetValue(fieldName, out var r))
            return r.ReadVector(docId);
        if (string.IsNullOrEmpty(fieldName) && _vectorReaders.Count == 1)
            return GetVector(docId);
        return null;
    }

    /// <summary>Returns the field names with vector data in this segment.</summary>
    public IReadOnlyCollection<string> VectorFieldNames => _vectorReaders.Keys;

    /// <summary>
    /// Returns the (lazy-loaded) HNSW graph for the given vector field, or null if no graph exists.
    /// Thread-safe; the first caller materialises the graph and subsequent callers reuse it.
    /// </summary>
    internal HnswGraph? GetHnswGraph(string fieldName)
    {
        if (_hnswGraphs.TryGetValue(fieldName, out var cached)) return cached;
        lock (_hnswLoadLock)
        {
            if (_hnswGraphs.TryGetValue(fieldName, out cached)) return cached;
            var path = VectorFilePaths.HnswFile(_basePath, fieldName);
            HnswGraph? graph = null;
            if (File.Exists(path) && _vectorReaders.TryGetValue(fieldName, out var vr))
            {
                var src = new VectorReaderSource(vr);
                bool? expectedNormalised = _info.VectorFields
                    .FirstOrDefault(vf => vf.FieldName == fieldName)?.Normalised;
                graph = HnswReader.Read(path, src, expectedNormalised);
            }
            _hnswGraphs[fieldName] = graph;
            return graph;
        }
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
