namespace Rowles.LeanLucene.Search;

/// <summary>
/// Base class for all query types.
/// </summary>
public abstract class Query
{
    public abstract string Field { get; }

    /// <summary>Boost factor applied to this query's score. Default 1.0.</summary>
    public float Boost { get; set; } = 1.0f;
}

/// <summary>
/// Boolean clause occurrence type.
/// </summary>
public enum Occur
{
    Must,
    Should,
    MustNot
}
