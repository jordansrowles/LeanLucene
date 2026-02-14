namespace Rowles.LeanLucene.Search.Queries;

/// <summary>
/// Matches all terms starting with a given prefix.
/// </summary>
public sealed class PrefixQuery : Query
{
    public override string Field { get; }
    public string Prefix { get; }

    public PrefixQuery(string field, string prefix)
    {
        Field = field;
        Prefix = prefix;
    }

    public override bool Equals(object? obj) =>
        obj is PrefixQuery other &&
        string.Equals(Field, other.Field, StringComparison.Ordinal) &&
        string.Equals(Prefix, other.Prefix, StringComparison.Ordinal) &&
        Boost == other.Boost;

    public override int GetHashCode() => CombineBoost(HashCode.Combine(nameof(PrefixQuery), Field, Prefix));
}
