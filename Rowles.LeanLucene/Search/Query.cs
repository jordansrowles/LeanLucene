namespace Rowles.LeanLucene.Search;

/// <summary>
/// Base class for all query types.
/// </summary>
public abstract class Query
{
    public abstract string Field { get; }
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
