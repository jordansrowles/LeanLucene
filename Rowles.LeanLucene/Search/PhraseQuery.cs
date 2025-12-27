namespace Rowles.LeanLucene.Search;

/// <summary>
/// Exact ordered phrase match using positional data.
/// </summary>
public sealed class PhraseQuery : Query
{
    public override string Field { get; }
    public string[] Terms { get; }

    public PhraseQuery(string field, params string[] terms)
    {
        Field = field;
        Terms = terms;
    }
}
