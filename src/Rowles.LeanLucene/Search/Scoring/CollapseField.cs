namespace Rowles.LeanLucene.Search.Scoring;

/// <summary>
/// Configuration for result collapsing (field grouping).
/// Keeps only the best document per unique value of the collapse field.
/// </summary>
public sealed record CollapseField(string FieldName, CollapseMode Mode = CollapseMode.TopScore);
