using Rowles.LeanLucene.Codecs;
using Rowles.LeanLucene.Codecs.Postings;
using Rowles.LeanLucene.Codecs.StoredFields;
using Rowles.LeanLucene.Store;

namespace Rowles.LeanLucene.Index.Indexer;

public sealed partial class IndexWriter
{
    /// <summary>
    /// Queues a term-based deletion. Documents matching <paramref name="query"/> are deleted
    /// on the next <see cref="Commit"/> call.
    /// </summary>
    /// <param name="query">The term query identifying documents to delete.</param>
    public void DeleteDocuments(TermQuery query)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
        lock (_writeLock)
        {
            _pendingDeletes.Add((query.Field, query.Term));
            _contentChangedSinceCommit = true;
        }
    }

    private void ApplyPendingDeletions(List<SegmentInfo> segments)
    {
        if (_pendingDeletes.Count == 0) return;

        var qualifiedTerms = new List<string>(_pendingDeletes.Count);
        foreach (var (field, term) in _pendingDeletes)
        {
            qualifiedTerms.Add(string.Concat(field, "\x00", term));
        }

        foreach (var seg in segments)
        {
            var basePath = Path.Combine(_directory.DirectoryPath, seg.SegmentId);
            var dicPath = basePath + ".dic";
            var posPath = basePath + ".pos";

            if (!File.Exists(dicPath) || !File.Exists(posPath))
                continue;

            using var dicReader = TermDictionaryReader.Open(dicPath);

            var delPath = basePath + ".del";
            var liveDocs = File.Exists(delPath)
                ? LiveDocs.Deserialise(delPath, seg.DocCount)
                : new LiveDocs(seg.DocCount);

            bool changed = false;
            using var posInput = new IndexInput(posPath);
            byte postingsVersion = PostingsEnum.ValidateFileHeader(posInput);

            foreach (var qualifiedTerm in qualifiedTerms)
            {
                if (!dicReader.TryGetPostingsOffset(qualifiedTerm, out long offset))
                    continue;

                ReadPostingsAtOffsetInto(posInput, offset, postingsVersion, liveDocs, ref changed);
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
    /// and marks matching live docs as deleted.
    /// </summary>
    private static void ReadPostingsAtOffsetInto(IndexInput input, long offset, byte postingsVersion, LiveDocs liveDocs, ref bool changed)
    {
        using var pe = PostingsEnum.Create(input, offset, postingsVersion);
        while (pe.MoveNext())
        {
            int docId = pe.DocId;
            if (liveDocs.IsLive(docId))
            {
                liveDocs.Delete(docId);
                changed = true;
            }
        }
    }
}
