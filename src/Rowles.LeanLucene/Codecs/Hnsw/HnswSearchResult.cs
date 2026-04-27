using Rowles.LeanLucene.Codecs;
using Rowles.LeanLucene.Codecs.Hnsw;
using Rowles.LeanLucene.Codecs.Fst;
using Rowles.LeanLucene.Codecs.Bkd;
using Rowles.LeanLucene.Codecs.Vectors;
using Rowles.LeanLucene.Codecs.TermVectors.TermVectors;
using Rowles.LeanLucene.Codecs.TermDictionary;
namespace Rowles.LeanLucene.Codecs.Hnsw;

/// <summary>
/// Result entry returned from HNSW search.
/// </summary>
internal readonly record struct HnswSearchResult(int DocId, float Score);
