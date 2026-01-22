namespace Rowles.LeanLucene.Search.Scoring;

/// <summary>
/// Specifies a field and direction for sorting search results.
/// </summary>
public sealed class SortField
{
    /// <summary>Sort by relevance score (default).</summary>
    public static readonly SortField Score = new(SortFieldType.Score, string.Empty);

    /// <summary>Sort by internal document ID (insertion order).</summary>
    public static readonly SortField DocId = new(SortFieldType.DocId, string.Empty);

    public SortFieldType Type { get; }
    public string FieldName { get; }
    public bool Descending { get; }

    public SortField(SortFieldType type, string fieldName, bool descending = false)
    {
        Type = type;
        FieldName = fieldName;
        Descending = descending;
    }

    /// <summary>Creates a numeric sort on the given field.</summary>
    public static SortField Numeric(string fieldName, bool descending = false)
        => new(SortFieldType.Numeric, fieldName, descending);

    /// <summary>Creates a string sort on the given field.</summary>
    public static SortField String(string fieldName, bool descending = false)
        => new(SortFieldType.String, fieldName, descending);
}

public enum SortFieldType
{
    /// <summary>Sort by BM25 relevance score (descending by default).</summary>
    Score,
    /// <summary>Sort by internal document ID.</summary>
    DocId,
    /// <summary>Sort by a numeric stored/indexed field.</summary>
    Numeric,
    /// <summary>Sort by a string stored field (lexicographic).</summary>
    String
}
