namespace Rowles.LeanLucene.Search.Queries;

/// <summary>A span: a contiguous range of positions in a document.</summary>
public readonly record struct Span(int DocId, int Start, int End);
