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
/// </summary>
public sealed class BKDReader : IDisposable
{
    private readonly FileStream _fs;
    private readonly BinaryReader _reader;
    private readonly Dictionary<string, long> _fieldOffsets;

    private BKDReader(FileStream fs, BinaryReader reader, Dictionary<string, long> fieldOffsets)
    {
        _fs = fs;
        _reader = reader;
        _fieldOffsets = fieldOffsets;
    }

    public static BKDReader Open(string filePath)
    {
        var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var reader = new BinaryReader(fs, System.Text.Encoding.UTF8, leaveOpen: true);

        CodecConstants.ValidateHeader(reader, CodecConstants.BKDVersion, "BKD tree (.bkd)");

        int fieldCount = reader.ReadInt32();
        var offsets = new Dictionary<string, long>(fieldCount, StringComparer.Ordinal);
        for (int f = 0; f < fieldCount; f++)
        {
            string fieldName = reader.ReadString();
            offsets[fieldName] = fs.Position;
            SkipNode(reader); // skip past this field's tree
        }

        return new BKDReader(fs, reader, offsets);
    }

    /// <summary>Returns all (docId, value) pairs in [min, max] range for the given field.</summary>
    public List<(int DocId, double Value)> RangeQuery(string field, double min, double max)
    {
        var results = new List<(int, double)>();
        if (!_fieldOffsets.TryGetValue(field, out long offset))
            return results;

        _fs.Seek(offset, SeekOrigin.Begin);
        SearchNode(_reader, min, max, results);
        return results;
    }

    public bool HasField(string field) => _fieldOffsets.ContainsKey(field);

    private static void SearchNode(BinaryReader reader, double min, double max, List<(int, double)> results)
    {
        byte marker = reader.ReadByte();
        if (marker == 1) // leaf
        {
            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                double value = reader.ReadDouble();
                int docId = reader.ReadInt32();
                if (value >= min && value <= max)
                    results.Add((docId, value));
            }
        }
        else // internal
        {
            double splitValue = reader.ReadDouble();
            if (min <= splitValue)
                SearchNode(reader, min, max, results); // left child
            else
                SkipNode(reader); // skip left child

            if (max >= splitValue)
                SearchNode(reader, min, max, results); // right child
            else
                SkipNode(reader); // skip right child
        }
    }

    private static void SkipNode(BinaryReader reader)
    {
        byte marker = reader.ReadByte();
        if (marker == 1) // leaf
        {
            int count = reader.ReadInt32();
            // each entry = 8 bytes (double) + 4 bytes (int) = 12 bytes
            reader.BaseStream.Seek(count * 12L, SeekOrigin.Current);
        }
        else // internal
        {
            reader.ReadDouble(); // split value
            SkipNode(reader); // left child
            SkipNode(reader); // right child
        }
    }

    public void Dispose()
    {
        _reader.Dispose();
        _fs.Dispose();
    }
}
