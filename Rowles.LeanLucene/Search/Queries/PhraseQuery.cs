namespace Rowles.LeanLucene.Search.Queries;

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

    public override bool Equals(object? obj) =>
        obj is PhraseQuery other &&
        string.Equals(Field, other.Field, StringComparison.Ordinal) &&
        Slop == other.Slop &&
        Boost == other.Boost &&
        Terms.AsSpan().SequenceEqual(other.Terms);

    public override int GetHashCode()
    {
        var h = new HashCode();
        h.Add(nameof(PhraseQuery));
        h.Add(Field);
        h.Add(Slop);
        foreach (var t in Terms) h.Add(t);
        return CombineBoost(h.ToHashCode());
    }
}
