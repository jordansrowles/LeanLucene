using Rowles.LeanLucene.Codecs;
using Rowles.LeanLucene.Store;

namespace Rowles.LeanLucene.Index.Indexer;

public sealed partial class IndexWriter
{
    public void DeleteDocuments(TermQuery query)
    {
        lock (_writeLock)
        {
            _pendingDeletes.Add((query.Field, query.Term));
        }
    }

    private void ApplyPendingDeletions(List<SegmentInfo> segments)
    {
        if (_pendingDeletes.Count == 0) return;

        var qualifiedTerms = new List<string>(_pendingDeletes.Count);
        foreach (var (field, term) in _pendingDeletes)
        {
            qualifiedTerms.Add($"{field}\x00{term}");
        }

        foreach (var seg in segments)
        {
            var basePath = Path.Combine(_directory.DirectoryPath, seg.SegmentId);
            var dicPath = basePath + ".dic";
            var posPath = basePath + ".pos";

            if (!File.Exists(dicPath) || !File.Exists(posPath))
                continue;

            using var dicReader = TermDictionaryReader.Open(dicPath);
            var liveDocs = new LiveDocs(seg.DocCount);

            var delPath = basePath + ".del";
            if (File.Exists(delPath))
                liveDocs = LiveDocs.Deserialise(delPath, seg.DocCount);

            bool changed = false;
            using var posInput = new IndexInput(posPath);
            foreach (var qualifiedTerm in qualifiedTerms)
            {
                if (!dicReader.TryGetPostingsOffset(qualifiedTerm, out long offset))
                    continue;

                ReadPostingsAtOffsetInto(posInput, offset, liveDocs, ref changed);
            }

            if (changed)
            {
                LiveDocs.Serialise(delPath, liveDocs);
                seg.LiveDocCount = liveDocs.LiveCount;
            }
        }

        _pendingDeletes.Clear();
    }

    /// <summary>
    /// Reads doc IDs from postings at the given offset using a memory-mapped IndexInput,
    /// and marks matching live docs as deleted. Zero allocation for the common case.
    /// </summary>
    private static void ReadPostingsAtOffsetInto(IndexInput input, long offset, LiveDocs liveDocs, ref bool changed)
    {
        input.Seek(offset);
        int count = input.ReadInt32();
        int skipCount = input.ReadInt32();
        if (skipCount > 0)
            input.Seek(input.Position + skipCount * 8L);

        int prev = 0;
        for (int i = 0; i < count; i++)
        {
            int delta = input.ReadVarInt();
            if (delta < 0)
                throw new InvalidDataException("Postings data is corrupt: negative delta encountered.");
            try
            {
                prev = checked(prev + delta);
            }
            catch (OverflowException ex)
            {
                throw new InvalidDataException("Postings data is corrupt: doc ID delta overflow.", ex);
            }
            if (liveDocs.IsLive(prev))
            {
                liveDocs.Delete(prev);
                changed = true;
            }
        }
    }
}
