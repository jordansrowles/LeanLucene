using Rowles.LeanLucene.Store;

namespace Rowles.LeanLucene.Codecs;

/// <summary>
/// Writes a sorted term dictionary with a sparse skip index for fast lookups.
/// Format: [int: skipEntryCount][skip entries][term entries].
/// Each skip entry: [int: termLength][chars: term][long: fileOffset].
/// Each term entry: [int: termLength][chars: term][long: postingsOffset].
/// </summary>
public static class TermDictionaryWriter
{
    private const int SkipInterval = 128;

    public static void Write(string filePath, List<string> sortedTerms, Dictionary<string, long> postingsOffsets)
    {
        // First pass: serialise term entries to a temporary buffer to compute offsets.
        using var termBuffer = new MemoryStream();
        using var termWriter = new BinaryWriter(termBuffer, System.Text.Encoding.UTF8, leaveOpen: true);

        var skipEntries = new List<(string Term, long Offset)>();

        for (int i = 0; i < sortedTerms.Count; i++)
        {
            if (i % SkipInterval == 0)
                skipEntries.Add((sortedTerms[i], termBuffer.Position));

            WriteTermEntry(termWriter, sortedTerms[i], postingsOffsets[sortedTerms[i]]);
        }

        termWriter.Flush();

        // Second pass: compute header size, adjust skip offsets, then write the file.
        long headerSize = ComputeHeaderSize(skipEntries);

        using var output = new IndexOutput(filePath);

        // Write skip index header.
        output.WriteInt32(skipEntries.Count);
        foreach (var (term, offset) in skipEntries)
        {
            output.WriteInt32(term.Length);
            var charBytes = System.Text.Encoding.UTF8.GetBytes(term.ToCharArray());
            output.WriteBytes(charBytes);
            output.WriteInt64(headerSize + offset);
        }

        // Write term entries from the pre-built buffer.
        output.WriteBytes(termBuffer.GetBuffer().AsSpan(0, (int)termBuffer.Length));
    }

    private static void WriteTermEntry(BinaryWriter writer, string term, long postingsOffset)
    {
        writer.Write(term.Length);
        writer.Write(term.ToCharArray());
        writer.Write(postingsOffset);
    }

    private static long ComputeHeaderSize(List<(string Term, long Offset)> skipEntries)
    {
        // int: skipEntryCount
        long size = sizeof(int);
        foreach (var (term, _) in skipEntries)
        {
            // int: termLength + UTF-8 encoded chars + long: fileOffset
            int charByteCount = System.Text.Encoding.UTF8.GetByteCount(term.ToCharArray());
            size += sizeof(int) + charByteCount + sizeof(long);
        }
        return size;
    }
}
