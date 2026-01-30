using Rowles.LeanLucene.Store;

namespace Rowles.LeanLucene.Codecs;

/// <summary>
/// Serialises a sorted term list into a compact byte-keyed v2 dictionary format.
/// Format: [termCount:int32][postingsOffsets: N×int64][keyStarts: (N+1)×int32][keyData: UTF-8 bytes].
/// This format enables O(log N) binary search on raw UTF-8 bytes without string materialisation.
/// Terms are re-sorted in UTF-8 byte order to ensure binary search correctness
/// (string ordinal sort can differ from UTF-8 byte sort for supplementary characters).
/// </summary>
internal static class FSTBuilder
{
    /// <summary>
    /// Writes sorted terms and their postings offsets in v2 format to the given <paramref name="output"/>.
    /// The codec header (magic + version) must already have been written by the caller.
    /// </summary>
    public static void Write(IndexOutput output, List<string> sortedTerms, Dictionary<string, long> postingsOffsets)
    {
        int count = sortedTerms.Count;
        output.WriteInt32(count);

        if (count == 0) return;

        // Pre-encode all terms to UTF-8
        var encoded = new byte[count][];
        for (int i = 0; i < count; i++)
            encoded[i] = System.Text.Encoding.UTF8.GetBytes(sortedTerms[i]);

        // Build index sorted by UTF-8 byte order (may differ from string ordinal for surrogates)
        var sortedIndices = new int[count];
        for (int i = 0; i < count; i++) sortedIndices[i] = i;
        Array.Sort(sortedIndices, (a, b) =>
            encoded[a].AsSpan().SequenceCompareTo(encoded[b]));

        // Compute key data layout in UTF-8 sorted order
        int totalBytes = 0;
        var starts = new int[count + 1];
        for (int i = 0; i < count; i++)
        {
            starts[i] = totalBytes;
            totalBytes += encoded[sortedIndices[i]].Length;
        }
        starts[count] = totalBytes;

        // Write postings offsets in UTF-8 sorted order
        for (int i = 0; i < count; i++)
            output.WriteInt64(postingsOffsets[sortedTerms[sortedIndices[i]]]);

        // Write key starts
        for (int i = 0; i <= count; i++)
            output.WriteInt32(starts[i]);

        // Write concatenated UTF-8 key data
        for (int i = 0; i < count; i++)
            output.WriteBytes(encoded[sortedIndices[i]]);
    }
}
