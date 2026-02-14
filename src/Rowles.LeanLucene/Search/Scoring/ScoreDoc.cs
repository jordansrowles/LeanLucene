namespace Rowles.LeanLucene.Search.Scoring;

/// <summary>
/// Represents a scored document in search results.
/// </summary>
public readonly record struct ScoreDoc(int DocId, float Score);
