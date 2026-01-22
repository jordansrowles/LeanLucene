namespace Rowles.LeanLucene.Search.Queries;

/// <summary>How to combine the inner query's BM25 score with the numeric field value.</summary>
public enum ScoreMode
{
    Multiply,
    Replace,
    Sum,
    Max
}

/// <summary>
/// Boosts an inner query's relevance score by a numeric field value.
/// </summary>
public sealed class FunctionScoreQuery : Query
{
    public Query Inner { get; }
    public string NumericField { get; }
    public ScoreMode Mode { get; }

    public override string Field => Inner.Field;

    public FunctionScoreQuery(Query inner, string numericField, ScoreMode mode = ScoreMode.Multiply)
    {
        ArgumentNullException.ThrowIfNull(inner);
        Inner = inner;
        NumericField = numericField;
        Mode = mode;
    }

    public static float Combine(float queryScore, double fieldValue, ScoreMode mode) => mode switch
    {
        ScoreMode.Multiply => queryScore * (float)fieldValue,
        ScoreMode.Replace => (float)fieldValue,
        ScoreMode.Sum => queryScore + (float)fieldValue,
        ScoreMode.Max => MathF.Max(queryScore, (float)fieldValue),
        _ => queryScore
    };
}
