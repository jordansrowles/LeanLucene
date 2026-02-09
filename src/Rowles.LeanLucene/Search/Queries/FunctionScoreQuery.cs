namespace Rowles.LeanLucene.Search.Queries;

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

    public override bool Equals(object? obj) =>
        obj is FunctionScoreQuery other &&
        Inner.Equals(other.Inner) &&
        string.Equals(NumericField, other.NumericField, StringComparison.Ordinal) &&
        Mode == other.Mode && Boost == other.Boost;

    public override int GetHashCode() =>
        CombineBoost(HashCode.Combine(nameof(FunctionScoreQuery), Inner, NumericField, Mode));

    public static float Combine(float queryScore, double fieldValue, ScoreMode mode) => mode switch
    {
        ScoreMode.Multiply => queryScore * (float)fieldValue,
        ScoreMode.Replace => (float)fieldValue,
        ScoreMode.Sum => queryScore + (float)fieldValue,
        ScoreMode.Max => MathF.Max(queryScore, (float)fieldValue),
        _ => queryScore
    };
}
