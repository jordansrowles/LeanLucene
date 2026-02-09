namespace Rowles.LeanLucene.Search.Queries;

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

    public override bool Equals(object? obj) =>
        obj is SpanNotQuery other &&
        Include.Equals(other.Include) && Exclude.Equals(other.Exclude) &&
        Boost == other.Boost;

    public override int GetHashCode() =>
        CombineBoost(HashCode.Combine(nameof(SpanNotQuery), Include, Exclude));
}
