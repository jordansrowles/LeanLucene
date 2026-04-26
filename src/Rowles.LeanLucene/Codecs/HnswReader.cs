namespace Rowles.LeanLucene.Codecs;

/// <summary>
/// Reads a <see cref="HnswGraph"/> previously written by <see cref="HnswWriter"/>. The graph
/// is materialised into the frozen, read-only state ready for concurrent search.
/// </summary>
internal static class HnswReader
{
    public static HnswGraph Read(string filePath, IVectorSource vectorSource)
    {
        using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var reader = new BinaryReader(fs, System.Text.Encoding.UTF8, leaveOpen: false);

        CodecConstants.ValidateHeader(reader, CodecConstants.HnswVersion, "HNSW");

        int dimension = reader.ReadInt32();
        bool normalised = reader.ReadByte() != 0;
        _ = normalised;
        if (vectorSource.Dimension != dimension)
            throw new InvalidDataException(
                $"HNSW file dimension {dimension} does not match vector source dimension {vectorSource.Dimension}.");

        int m = reader.ReadInt32();
        int m0 = reader.ReadInt32();
        int efConstruction = reader.ReadInt32();
        long seed = reader.ReadInt64();
        int entryPoint = reader.ReadInt32();
        int maxLevel = reader.ReadInt32();
        int nodeCount = reader.ReadInt32();
        int levelCount = reader.ReadInt32();

        var levels = new List<Dictionary<int, int[]>>(levelCount);
        for (int i = 0; i < levelCount; i++)
            levels.Add(new Dictionary<int, int[]>());

        for (int level = levelCount - 1; level >= 0; level--)
        {
            int nodes = reader.ReadInt32();
            var dict = levels[level];
            for (int n = 0; n < nodes; n++)
            {
                int docId = reader.ReadInt32();
                int neighCount = reader.ReadInt32();
                var arr = new int[neighCount];
                for (int k = 0; k < neighCount; k++)
                    arr[k] = reader.ReadInt32();
                dict[docId] = arr;
            }
        }

        var config = new HnswBuildConfig { M = m, M0 = m0, EfConstruction = efConstruction };
        return HnswGraph.FromFrozen(vectorSource, config, seed, levels, entryPoint, maxLevel, nodeCount);
    }
}
