namespace Rowles.LeanLucene.Codecs;

/// <summary>A single term vector entry: term text, frequency in the document, and positions.</summary>
public readonly record struct TermVectorEntry(string Term, int Freq, int[] Positions);
