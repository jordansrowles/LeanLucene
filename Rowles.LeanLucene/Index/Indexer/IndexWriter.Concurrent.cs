using Rowles.LeanLucene.Analysis;

namespace Rowles.LeanLucene.Index.Indexer;

public sealed partial class IndexWriter
{
    public void AddDocumentsConcurrent(IReadOnlyList<Document.LeanDocument> documents)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
        if (documents.Count == 0) return;

        var perThreadResults = new System.Collections.Concurrent.ConcurrentBag<DocumentsWriterPerThread>();

        Parallel.ForEach(
            System.Collections.Concurrent.Partitioner.Create(0, documents.Count),
            () => CreateThreadLocalDocumentWriter(),
            (range, _, dwpt) =>
            {
                for (int i = range.Item1; i < range.Item2; i++)
                    dwpt.AddDocument(documents[i], i);
                return dwpt;
            },
            dwpt => perThreadResults.Add(dwpt));

        lock (_writeLock)
        {
            foreach (var dwpt in perThreadResults)
                MergeDwpt(dwpt);
        }
    }

    /// <summary>
    /// Creates a DocumentsWriterPerThread with fresh analyser instances for thread-safe parallel indexing.
    /// </summary>
    private DocumentsWriterPerThread CreateThreadLocalDocumentWriter()
    {
        IAnalyser threadLocalDefaultAnalyser = _defaultAnalyser switch
        {
            StandardAnalyser => new StandardAnalyser(_config.AnalyserInternCacheSize, _config.StopWords),
            StemmedAnalyser => new StemmedAnalyser(),
            _ => _defaultAnalyser
        };

        var threadLocalFieldAnalysers = new Dictionary<string, IAnalyser>(_config.FieldAnalysers.Count);
        foreach (var kvp in _config.FieldAnalysers)
        {
            threadLocalFieldAnalysers[kvp.Key] = kvp.Value switch
            {
                StandardAnalyser => new StandardAnalyser(),
                StemmedAnalyser => new StemmedAnalyser(),
                _ => kvp.Value
            };
        }

        return new DocumentsWriterPerThread(threadLocalDefaultAnalyser, threadLocalFieldAnalysers);
    }

    private void MergeDwpt(DocumentsWriterPerThread dwpt)
    {
        int docBase = _bufferedDocCount;
        foreach (var (qt, srcAcc) in dwpt.Postings)
        {
            if (!_postings.TryGetValue(qt, out var dstAcc))
            {
                dstAcc = new PostingAccumulator();
                _postings[qt] = dstAcc;
            }
            var srcIds = srcAcc.DocIds;
            for (int i = 0; i < srcIds.Length; i++)
            {
                int remappedDocId = srcIds[i] + docBase;
                if (srcAcc.HasPositions)
                {
                    var positions = srcAcc.GetPositions(i);
                    foreach (var p in positions)
                        dstAcc.Add(remappedDocId, p);
                }
                else
                {
                    dstAcc.AddDocOnly(remappedDocId);
                }
            }
        }

        foreach (var storedDoc in dwpt.StoredFields)
        {
            _sfDocStarts.Add(_sfFieldIds.Count);
            foreach (var (name, values) in storedDoc)
            {
                foreach (var value in values)
                    AppendStoredField(name, value);
            }
        }

        foreach (var (fieldName, counts) in dwpt.DocTokenCounts)
        {
            if (!_docTokenCounts.TryGetValue(fieldName, out var dstCounts))
            {
                dstCounts = new int[_config.MaxBufferedDocs];
                _docTokenCounts[fieldName] = dstCounts;
            }
            
            int newTotal = docBase + dwpt.DocCount;
            if (newTotal > dstCounts.Length)
            {
                Array.Resize(ref dstCounts, Math.Max(dstCounts.Length * 2, newTotal));
                _docTokenCounts[fieldName] = dstCounts;
            }
            
            for (int i = 0; i < dwpt.DocCount && i < counts.Length; i++)
                dstCounts[docBase + i] = counts[i];
        }

        foreach (var fn in dwpt.FieldNames)
            _fieldNames.Add(fn);

        foreach (var (field, map) in dwpt.NumericIndex)
        {
            if (!_numericIndex.TryGetValue(field, out var dstMap))
            {
                dstMap = new Dictionary<int, double>();
                _numericIndex[field] = dstMap;
            }
            foreach (var (docId, val) in map)
                dstMap[docId + docBase] = val;
        }

        foreach (var (field, list) in dwpt.NumericDocValues)
        {
            if (!_numericDocValues.TryGetValue(field, out var dstList))
            {
                dstList = new List<double>();
                _numericDocValues[field] = dstList;
            }
            while (dstList.Count < docBase) dstList.Add(0);
            dstList.AddRange(list);
        }

        _bufferedDocCount += dwpt.DocCount;
        _estimatedRamBytes += dwpt.DocCount * 200;

        if (ShouldFlush())
            FlushSegment();
    }
}
