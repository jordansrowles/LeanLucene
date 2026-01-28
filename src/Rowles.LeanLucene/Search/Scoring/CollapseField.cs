namespace Rowles.LeanLucene.Search.Scoring;

/// <summary>
/// Configuration for result collapsing (field grouping).
/// Keeps only the best document per unique value of the collapse field.
/// </summary>
public sealed record CollapseField(string FieldName, CollapseMode Mode = CollapseMode.TopScore);

/// <summary>How to select the representative document per group.</summary>
public enum CollapseMode
{
    /// <summary>Keep the highest-scoring document per group.</summary>
    TopScore,

    /// <summary>Keep the lowest-scoring document per group (useful for ascending sort).</summary>
    MinScore
}
