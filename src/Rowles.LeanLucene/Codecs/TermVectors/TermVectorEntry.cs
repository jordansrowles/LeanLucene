using Rowles.LeanLucene.Codecs;
using Rowles.LeanLucene.Codecs.Hnsw;
using Rowles.LeanLucene.Codecs.Fst;
using Rowles.LeanLucene.Codecs.Bkd;
using Rowles.LeanLucene.Codecs.Vectors;
using Rowles.LeanLucene.Codecs.TermVectors.TermVectors;
using Rowles.LeanLucene.Codecs.TermDictionary;
namespace Rowles.LeanLucene.Codecs.TermVectors.TermVectors;

/// <summary>A single term vector entry: term text, frequency in the document, and positions.</summary>
/// <param name="Term">The term text.</param>
/// <param name="Freq">The number of times the term appears in the document.</param>
/// <param name="Positions">The token positions at which the term appears.</param>
public readonly record struct TermVectorEntry(string Term, int Freq, int[] Positions);
