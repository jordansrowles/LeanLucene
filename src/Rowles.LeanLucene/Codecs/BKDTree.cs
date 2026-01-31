namespace Rowles.LeanLucene.Codecs;

/// <summary>
/// Writes a 1-dimensional BKD tree for numeric point values, enabling O(log N + results) range lookups.
/// File format (.bkd): [fieldCount:int32] per field: [fieldName:string] [nodeCount:int32] [nodes...]
/// Each leaf stores sorted (value, docId) pairs; internal nodes store split values.
/// </summary>
public static class BKDWriter
{
    /// <summary>Default max leaf size for BKD tree nodes.</summary>
    public const int DefaultMaxLeafSize = 512;

    internal static void Write(string filePath, Dictionary<string, List<(double Value, int DocId)>> fieldPoints, int maxLeafSize = DefaultMaxLeafSize)
    {
        using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
        using var writer = new BinaryWriter(fs, System.Text.Encoding.UTF8, leaveOpen: false);

        CodecConstants.WriteHeader(writer, CodecConstants.BKDVersion);

        writer.Write(fieldPoints.Count);
        foreach (var (field, points) in fieldPoints)
        {
            writer.Write(field);
            points.Sort((a, b) => a.Value.CompareTo(b.Value));
            WriteNode(writer, points, 0, points.Count, maxLeafSize);
        }
    }

    private static void WriteNode(BinaryWriter writer, List<(double Value, int DocId)> points, int start, int end, int maxLeafSize)
    {
        int count = end - start;
        if (count <= maxLeafSize)
        {
            // Leaf node
            writer.Write((byte)1); // leaf marker
            writer.Write(count);
            for (int i = start; i < end; i++)
            {
                writer.Write(points[i].Value);
                writer.Write(points[i].DocId);
            }
        }
        else
        {
            // Internal node — split at median
            int mid = start + count / 2;
            writer.Write((byte)0); // internal marker
            writer.Write(points[mid].Value); // split value
            WriteNode(writer, points, start, mid, maxLeafSize);
            WriteNode(writer, points, mid, end, maxLeafSize);
        }
    }
}

/// <summary>
/// Reads a 1-dimensional BKD tree for efficient numeric range lookups.
/// Uses memory-mapped IndexInput for zero-copy seeks.
/// </summary>
public sealed class BKDReader : IDisposable
{
    private readonly Store.IndexInput _input;
    private readonly Dictionary<string, long> _fieldOffsets;

    private BKDReader(Store.IndexInput input, Dictionary<string, long> fieldOffsets)
    {
        _input = input;
        _fieldOffsets = fieldOffsets;
    }

    public static BKDReader Open(string filePath)
    {
        var input = new Store.IndexInput(filePath);

        CodecConstants.ValidateHeader(input, CodecConstants.BKDVersion, "BKD tree (.bkd)");

        int fieldCount = input.ReadInt32();
        var offsets = new Dictionary<string, long>(fieldCount, StringComparer.Ordinal);
        for (int f = 0; f < fieldCount; f++)
        {
            string fieldName = input.ReadLengthPrefixedString();
            offsets[fieldName] = input.Position;
            SkipNode(input);
        }

        return new BKDReader(input, offsets);
    }

    /// <summary>Returns all (docId, value) pairs in [min, max] range for the given field.</summary>
    public List<(int DocId, double Value)> RangeQuery(string field, double min, double max)
    {
        var results = new List<(int, double)>();
        if (!_fieldOffsets.TryGetValue(field, out long offset))
            return results;

        _input.Seek(offset);
        SearchNode(_input, min, max, results);
        return results;
    }

    public bool HasField(string field) => _fieldOffsets.ContainsKey(field);

    private static void SearchNode(Store.IndexInput input, double min, double max, List<(int, double)> results)
    {
        byte marker = input.ReadByte();
        if (marker == 1) // leaf
        {
            int count = input.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                double value = input.ReadDouble();
                int docId = input.ReadInt32();
                if (value >= min && value <= max)
                    results.Add((docId, value));
            }
        }
        else // internal
        {
            double splitValue = input.ReadDouble();
            if (min <= splitValue)
                SearchNode(input, min, max, results);
            else
                SkipNode(input);

            if (max >= splitValue)
                SearchNode(input, min, max, results);
            else
                SkipNode(input);
        }
    }

    private static void SkipNode(Store.IndexInput input)
    {
        byte marker = input.ReadByte();
        if (marker == 1) // leaf
        {
            int count = input.ReadInt32();
            input.Seek(input.Position + count * 12L);
        }
        else // internal
        {
            input.ReadDouble(); // split value
            SkipNode(input);
            SkipNode(input);
        }
    }

    public void Dispose() => _input.Dispose();
}
