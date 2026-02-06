using Rowles.LeanLucene.Search.Scoring;

namespace Rowles.LeanLucene.Index.Indexer;

/// <summary>
/// Defines the sort order applied to documents within a segment at flush time.
/// When configured, documents are physically reordered before writing, enabling
/// early termination during sorted searches.
/// </summary>
public sealed class IndexSort : IEquatable<IndexSort>
{
    public IReadOnlyList<SortField> Fields { get; }

    public IndexSort(params SortField[] fields)
    {
        if (fields.Length == 0)
            throw new ArgumentException("At least one sort field is required.", nameof(fields));
        foreach (var f in fields)
        {
            if (f.Type == SortFieldType.Score)
                throw new ArgumentException("Index sort cannot use Score sort type.", nameof(fields));
        }
        Fields = fields.ToArray();
    }

    public bool Equals(IndexSort? other)
    {
        if (other is null || Fields.Count != other.Fields.Count) return false;
        for (int i = 0; i < Fields.Count; i++)
        {
            var a = Fields[i];
            var b = other.Fields[i];
            if (a.Type != b.Type || a.FieldName != b.FieldName || a.Descending != b.Descending)
                return false;
        }
        return true;
    }

    public override bool Equals(object? obj) => Equals(obj as IndexSort);

    public override int GetHashCode()
    {
        var hc = new HashCode();
        foreach (var f in Fields)
        {
            hc.Add(f.Type);
            hc.Add(f.FieldName);
            hc.Add(f.Descending);
        }
        return hc.ToHashCode();
    }

    public override string ToString()
        => string.Join(", ", Fields.Select(f => $"{f.FieldName}:{f.Type}{(f.Descending ? " DESC" : "")}"));
}
