namespace Rowles.LeanLucene.Search.Queries;

/// <summary>Base type for position-aware queries that produce spans.</summary>
public abstract class SpanQuery : Query
{
    public abstract override string Field { get; }
}
