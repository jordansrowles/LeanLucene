namespace Rowles.LeanLucene.Search;

/// <summary>A span: a contiguous range of positions in a document.</summary>
public readonly record struct Span(int DocId, int Start, int End);

/// <summary>Base type for position-aware queries that produce spans.</summary>
public abstract class SpanQuery : Query
{
    public abstract override string Field { get; }
}

/// <summary>A span query matching a single term.</summary>
public sealed class SpanTermQuery : SpanQuery
{
    public override string Field { get; }
    public string Term { get; }

    /// <summary>Cached qualified term to avoid repeated string.Concat.</summary>
    internal string? CachedQualifiedTerm { get; set; }

    public SpanTermQuery(string field, string term)
    {
        Field = field;
        Term = term;
    }
}

/// <summary>
/// Matches documents where the spans from all clauses appear within <see cref="Slop"/> positions
/// of each other. When <see cref="InOrder"/> is true, the spans must appear in the given order.
/// </summary>
public sealed class SpanNearQuery : SpanQuery
{
    public IReadOnlyList<SpanQuery> Clauses { get; }
    public int Slop { get; }
    public bool InOrder { get; }

    public override string Field => Clauses.Count > 0 ? Clauses[0].Field : string.Empty;

    public SpanNearQuery(SpanQuery[] clauses, int slop, bool inOrder = true)
    {
        ArgumentNullException.ThrowIfNull(clauses);
        ArgumentOutOfRangeException.ThrowIfNegative(slop);
        Clauses = clauses;
        Slop = slop;
        InOrder = inOrder;
    }
}

/// <summary>Matches documents that match any of the provided span sub-queries.</summary>
public sealed class SpanOrQuery : SpanQuery
{
    public IReadOnlyList<SpanQuery> Clauses { get; }

    public override string Field => Clauses.Count > 0 ? Clauses[0].Field : string.Empty;

    public SpanOrQuery(params SpanQuery[] clauses)
    {
        ArgumentNullException.ThrowIfNull(clauses);
        Clauses = clauses;
    }
}

/// <summary>
/// Matches spans from <see cref="Include"/> that do not overlap with any span from <see cref="Exclude"/>.
/// </summary>
public sealed class SpanNotQuery : SpanQuery
{
    public SpanQuery Include { get; }
    public SpanQuery Exclude { get; }

    public override string Field => Include.Field;

    public SpanNotQuery(SpanQuery include, SpanQuery exclude)
    {
        ArgumentNullException.ThrowIfNull(include);
        ArgumentNullException.ThrowIfNull(exclude);
        Include = include;
        Exclude = exclude;
    }
}
