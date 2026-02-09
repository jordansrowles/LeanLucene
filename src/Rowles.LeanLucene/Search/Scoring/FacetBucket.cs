namespace Rowles.LeanLucene.Search.Scoring;

/// <summary>A single facet bucket: a field value and how many matching documents have it.</summary>
public readonly record struct FacetBucket(string Value, int Count);
