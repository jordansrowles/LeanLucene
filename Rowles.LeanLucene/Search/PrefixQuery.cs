namespace Rowles.LeanLucene.Search;

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
}
