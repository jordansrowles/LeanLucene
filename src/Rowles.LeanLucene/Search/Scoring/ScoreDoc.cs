namespace Rowles.LeanLucene.Search.Scoring;

/// <summary>
/// Represents a scored document in search results.
/// </summary>
/// <param name="DocId">The zero-based internal document identifier.</param>
/// <param name="Score">The relevance score assigned to this document.</param>
public readonly record struct ScoreDoc(int DocId, float Score);
