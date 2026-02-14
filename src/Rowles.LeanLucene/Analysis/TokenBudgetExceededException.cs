namespace Rowles.LeanLucene.Analysis;

/// <summary>
/// Thrown when <see cref="TokenBudgetPolicy.Reject"/> is active and a document exceeds the token budget.
/// </summary>
public sealed class TokenBudgetExceededException : InvalidOperationException
{
    public int TokenCount { get; }
    public int Budget { get; }

    public TokenBudgetExceededException(int tokenCount, int budget)
        : base($"Document produced {tokenCount} tokens, exceeding the budget of {budget}.")
    {
        TokenCount = tokenCount;
        Budget = budget;
    }
}
