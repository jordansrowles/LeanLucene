using Rowles.LeanLucene.Search.Queries;

using Rowles.LeanLucene.Search;
using Rowles.LeanLucene.Search.Simd;
using Rowles.LeanLucene.Search.Parsing;
using Rowles.LeanLucene.Search.Highlighting;
namespace Rowles.LeanLucene.Search.Parsing;

/// <summary>
/// Fluent builder for <see cref="BooleanQuery"/> clauses.
/// </summary>
public sealed class BooleanQueryBuilder
{
    private readonly BooleanQuery _query = new();

    /// <summary>Adds a MUST clause.</summary>
    public BooleanQueryBuilder Must(Query query)
    {
        _query.Add(query, Occur.Must);
        return this;
    }

    /// <summary>Adds a SHOULD clause.</summary>
    public BooleanQueryBuilder Should(Query query)
    {
        _query.Add(query, Occur.Should);
        return this;
    }

    /// <summary>Adds a MUST_NOT clause.</summary>
    public BooleanQueryBuilder MustNot(Query query)
    {
        _query.Add(query, Occur.MustNot);
        return this;
    }

    internal BooleanQuery Build() => _query;
}
