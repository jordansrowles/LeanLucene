using Rowles.LeanLucene.Analysis;
using Rowles.LeanLucene.Document;

namespace Rowles.LeanLucene.Index;

/// <summary>
/// A per-thread document buffer for concurrent indexing.
/// Each thread accumulates postings independently; the writer merges them on flush.
/// </summary>
internal sealed class DocumentsWriterPerThread
{
    private readonly IAnalyser _analyser;
    private readonly Dictionary<string, IAnalyser> _fieldAnalysers;
    internal readonly Dictionary<string, PostingAccumulator> Postings = new(StringComparer.Ordinal);
    internal readonly List<Dictionary<string, List<string>>> StoredFields = [];
    internal readonly Dictionary<string, Dictionary<int, double>> NumericIndex = new();
    internal readonly Dictionary<string, List<double>> NumericDocValues = new(StringComparer.Ordinal);
    internal readonly Dictionary<string, List<string?>> SortedDocValues = new(StringComparer.Ordinal);
    internal readonly HashSet<string> FieldNames = new(StringComparer.Ordinal);
    // Per-field token counts: field → docId → count
    internal Dictionary<string, int[]> DocTokenCounts = new(StringComparer.Ordinal);
    internal int DocCount;
    private readonly Dictionary<(string, string), string> _qualifiedTermPool = new();
    private readonly HashSet<string> _termPool = new(StringComparer.Ordinal);

    public DocumentsWriterPerThread(IAnalyser defaultAnalyser, Dictionary<string, IAnalyser> fieldAnalysers)
    {
        _analyser = defaultAnalyser;
        _fieldAnalysers = fieldAnalysers;
    }

    /// <summary>
    /// Indexes a single document into this thread's local buffer.
    /// Not thread-safe — each thread owns its own DWPT instance.
    /// </summary>
    public void AddDocument(LeanDocument doc, int globalDocId)
    {
        int localDocId = DocCount;
        var storedDoc = new Dictionary<string, List<string>>();

        foreach (var field in doc.Fields)
        {
            switch (field)
            {
                case TextField tf:
                    IndexTextField(tf.Name, tf.Value, localDocId);
                    if (tf.IsStored)
                    {
                        if (!storedDoc.TryGetValue(tf.Name, out var list))
                        {
                            list = new List<string>();
                            storedDoc[tf.Name] = list;
                        }
                        list.Add(tf.Value);
                    }
                    break;
                case StringField sf:
                    IndexStringField(sf.Name, sf.Value, localDocId);
                    if (sf.IsStored)
                    {
                        if (!storedDoc.TryGetValue(sf.Name, out var list))
                        {
                            list = new List<string>();
                            storedDoc[sf.Name] = list;
                        }
                        list.Add(sf.Value);
                    }
                    break;
                case NumericField nf:
                    IndexNumericField(nf.Name, nf.Value, localDocId);
                    if (nf.IsStored)
                    {
                        if (!storedDoc.TryGetValue(nf.Name, out var list))
                        {
                            list = new List<string>();
                            storedDoc[nf.Name] = list;
                        }
                        list.Add(nf.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    }
                    break;
            }
        }

        StoredFields.Add(storedDoc);
        DocCount++;
    }

    private void IndexTextField(string fieldName, string value, int docId)
    {
        var analyser = _fieldAnalysers.GetValueOrDefault(fieldName, _analyser);
        var tokens = analyser.Analyse(value.AsSpan());

        // Track per-field token counts
        if (!DocTokenCounts.TryGetValue(fieldName, out var counts))
        {
            counts = new int[16];
            DocTokenCounts[fieldName] = counts;
        }
        if (docId >= counts.Length)
            Array.Resize(ref counts, Math.Max(counts.Length * 2, docId + 1));
        counts[docId] += tokens.Count;
        DocTokenCounts[fieldName] = counts; // Update reference in case of resize
        
        FieldNames.Add(fieldName);

        for (int pos = 0; pos < tokens.Count; pos++)
        {
            var term = CanonicaliseTerm(tokens[pos].Text);
            var qualifiedTerm = GetQualifiedTerm(fieldName, term);

            if (!Postings.TryGetValue(qualifiedTerm, out var acc))
            {
                acc = new PostingAccumulator();
                Postings[qualifiedTerm] = acc;
            }
            acc.Add(docId, pos);
        }
    }

    private void IndexStringField(string fieldName, string value, int docId)
    {
        FieldNames.Add(fieldName);
        var term = CanonicaliseTerm(value);
        var qualifiedTerm = GetQualifiedTerm(fieldName, term);

        if (!Postings.TryGetValue(qualifiedTerm, out var acc))
        {
            acc = new PostingAccumulator();
            Postings[qualifiedTerm] = acc;
        }
        acc.AddDocOnly(docId);
    }

    private void IndexNumericField(string fieldName, double value, int docId)
    {
        if (!NumericIndex.TryGetValue(fieldName, out var fieldMap))
        {
            fieldMap = new Dictionary<int, double>();
            NumericIndex[fieldName] = fieldMap;
        }
        fieldMap[docId] = value;

        if (!NumericDocValues.TryGetValue(fieldName, out var dvList))
        {
            dvList = new List<double>();
            NumericDocValues[fieldName] = dvList;
        }
        while (dvList.Count < docId) dvList.Add(0);
        dvList.Add(value);
    }

    private string CanonicaliseTerm(string term)
    {
        if (_termPool.TryGetValue(term, out var canonical))
            return canonical;
        _termPool.Add(term);
        return term;
    }

    private string GetQualifiedTerm(string fieldName, string term)
    {
        if (!_qualifiedTermPool.TryGetValue((fieldName, term), out var qt))
        {
            qt = string.Concat(fieldName, "\x00", term);
            _qualifiedTermPool[(fieldName, term)] = qt;
        }
        return qt;
    }
}
