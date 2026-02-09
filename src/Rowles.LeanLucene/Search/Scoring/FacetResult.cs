namespace Rowles.LeanLucene.Search.Scoring;

/// <summary>The facet result for one field: the field name and its value-count buckets.</summary>
public sealed class FacetResult
{
    public string FieldName { get; }
    public IReadOnlyList<FacetBucket> Buckets { get; }

    public FacetResult(string fieldName, IReadOnlyList<FacetBucket> buckets)
    {
        FieldName = fieldName;
        Buckets = buckets;
    }
}
