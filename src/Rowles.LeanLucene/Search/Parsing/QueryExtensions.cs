using Rowles.LeanLucene.Search.Queries;

using Rowles.LeanLucene.Search;
using Rowles.LeanLucene.Search.Simd;
using Rowles.LeanLucene.Search.Parsing;
using Rowles.LeanLucene.Search.Highlighting;
namespace Rowles.LeanLucene.Search.Parsing;

/// <summary>
/// Extension methods for fluent query composition on <see cref="Query"/>.
/// </summary>
public static class QueryExtensions
{
    /// <summary>Sets the boost on the query and returns it for chaining.</summary>
    public static T WithBoost<T>(this T query, float boost) where T : Query
    {
        query.Boost = boost;
        return query;
    }

    /// <summary>Combines two queries with a MUST boolean clause.</summary>
    public static BooleanQuery And(this Query left, Query right)
    {
        var bq = new BooleanQuery();
        bq.Add(left, Occur.Must);
        bq.Add(right, Occur.Must);
        return bq;
    }

    /// <summary>Combines two queries with a SHOULD boolean clause.</summary>
    public static BooleanQuery Or(this Query left, Query right)
    {
        var bq = new BooleanQuery();
        bq.Add(left, Occur.Should);
        bq.Add(right, Occur.Should);
        return bq;
    }

    /// <summary>Excludes results matching the given query.</summary>
    public static BooleanQuery Not(this Query left, Query excluded)
    {
        var bq = new BooleanQuery();
        bq.Add(left, Occur.Must);
        bq.Add(excluded, Occur.MustNot);
        return bq;
    }
}
