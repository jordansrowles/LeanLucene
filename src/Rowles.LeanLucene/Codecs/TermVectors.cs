namespace Rowles.LeanLucene.Codecs;

/// <summary>A single term vector entry: term text, frequency in the document, and positions.</summary>
public readonly record struct TermVectorEntry(string Term, int Freq, int[] Positions);

/// <summary>
/// Writes per-document term vectors to .tvd (data) and .tvx (offset index) files.
/// Format: .tvx: [docCount:int32] [long[] offsets into .tvd]
///         .tvd per doc: [fieldCount:int32] per field: [fieldName:string] [termCount:int32]
///              per term: [term:string] [freq:int32] [posCount:int32] [positions:int32[]]
/// </summary>
public static class TermVectorsWriter
{
    public static void Write(string tvdPath, string tvxPath,
        IReadOnlyList<Dictionary<string, List<TermVectorEntry>>> docs)
    {
        using var tvdFs = new FileStream(tvdPath, FileMode.Create, FileAccess.Write, FileShare.None);
        using var tvdWriter = new BinaryWriter(tvdFs, System.Text.Encoding.UTF8, leaveOpen: false);

        CodecConstants.WriteHeader(tvdWriter, CodecConstants.TermVectorsVersion);

        var offsets = new long[docs.Count];

        for (int d = 0; d < docs.Count; d++)
        {
            offsets[d] = tvdFs.Position;
            var fields = docs[d];
            tvdWriter.Write(fields.Count);
            foreach (var (fieldName, entries) in fields)
            {
                tvdWriter.Write(fieldName);
                tvdWriter.Write(entries.Count);
                foreach (var entry in entries)
                {
                    tvdWriter.Write(entry.Term);
                    tvdWriter.Write(entry.Freq);
                    tvdWriter.Write(entry.Positions.Length);
                    foreach (var pos in entry.Positions)
                        tvdWriter.Write(pos);
                }
            }
        }

        // Write .tvx index
        using var tvxFs = new FileStream(tvxPath, FileMode.Create, FileAccess.Write, FileShare.None);
        using var tvxWriter = new BinaryWriter(tvxFs, System.Text.Encoding.UTF8, leaveOpen: false);
        
        CodecConstants.WriteHeader(tvxWriter, CodecConstants.TermVectorsVersion);
        
        tvxWriter.Write(docs.Count);
        foreach (var offset in offsets)
            tvxWriter.Write(offset);
    }
}

/// <summary>Reads per-document term vectors from .tvd/.tvx files using memory-mapped I/O.</summary>
public sealed class TermVectorsReader : IDisposable
{
    private readonly Store.IndexInput _tvdInput;
    private readonly long[] _offsets;

    private TermVectorsReader(Store.IndexInput tvdInput, long[] offsets)
    {
        _tvdInput = tvdInput;
        _offsets = offsets;
    }

    public static TermVectorsReader Open(string tvdPath, string tvxPath)
    {
        // Read offsets from .tvx index file
        using var tvxInput = new Store.IndexInput(tvxPath);
        CodecConstants.ValidateHeader(tvxInput, CodecConstants.TermVectorsVersion, "term vectors index (.tvx)");

        int docCount = tvxInput.ReadInt32();
        var offsets = new long[docCount];
        for (int i = 0; i < docCount; i++)
            offsets[i] = tvxInput.ReadInt64();

        // Open .tvd data file as mmap
        var tvdInput = new Store.IndexInput(tvdPath);
        CodecConstants.ValidateHeader(tvdInput, CodecConstants.TermVectorsVersion, "term vectors data (.tvd)");

        return new TermVectorsReader(tvdInput, offsets);
    }

    /// <summary>Returns all term vectors for a document across all stored fields.</summary>
    public Dictionary<string, List<TermVectorEntry>> GetTermVector(int docId)
    {
        if ((uint)docId >= (uint)_offsets.Length)
            return new();

        _tvdInput.Seek(_offsets[docId]);
        int fieldCount = _tvdInput.ReadInt32();
        var result = new Dictionary<string, List<TermVectorEntry>>(fieldCount, StringComparer.Ordinal);

        for (int f = 0; f < fieldCount; f++)
        {
            string fieldName = _tvdInput.ReadLengthPrefixedString();
            int termCount = _tvdInput.ReadInt32();
            var entries = new List<TermVectorEntry>(termCount);
            for (int t = 0; t < termCount; t++)
            {
                string term = _tvdInput.ReadLengthPrefixedString();
                int freq = _tvdInput.ReadInt32();
                int posCount = _tvdInput.ReadInt32();
                var positions = new int[posCount];
                for (int p = 0; p < posCount; p++)
                    positions[p] = _tvdInput.ReadInt32();
                entries.Add(new TermVectorEntry(term, freq, positions));
            }
            result[fieldName] = entries;
        }

        return result;
    }

    /// <summary>Returns term vectors for a specific field in a document, or null if unavailable.</summary>
    public IReadOnlyList<TermVectorEntry>? GetTermVector(int docId, string field)
    {
        var all = GetTermVector(docId);
        return all.GetValueOrDefault(field);
    }

    public void Dispose() => _tvdInput.Dispose();
}
