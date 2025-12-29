namespace Rowles.LeanLucene.Search;

/// <summary>
/// Exact ordered phrase match using positional data, with optional slop.
/// </summary>
public sealed class PhraseQuery : Query
{
    public override string Field { get; }
    public string[] Terms { get; }

    /// <summary>Maximum number of positional gaps allowed between terms. 0 = exact phrase.</summary>
    public int Slop { get; set; }

    public PhraseQuery(string field, params string[] terms)
    {
        Field = field;
        Terms = terms;
    }

    public PhraseQuery(string field, int slop, params string[] terms)
    {
        Field = field;
        Slop = slop;
        Terms = terms;
    }
}
