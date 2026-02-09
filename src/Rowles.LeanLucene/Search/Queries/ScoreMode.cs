namespace Rowles.LeanLucene.Search.Queries;

/// <summary>How to combine the inner query's BM25 score with the numeric field value.</summary>
public enum ScoreMode
{
    Multiply,
    Replace,
    Sum,
    Max
}
