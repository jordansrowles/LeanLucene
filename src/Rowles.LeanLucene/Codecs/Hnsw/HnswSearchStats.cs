using Rowles.LeanLucene.Codecs;
using Rowles.LeanLucene.Codecs.Hnsw;
using Rowles.LeanLucene.Codecs.Fst;
using Rowles.LeanLucene.Codecs.Bkd;
using Rowles.LeanLucene.Codecs.Vectors;
using Rowles.LeanLucene.Codecs.TermVectors;
using Rowles.LeanLucene.Codecs.TermDictionary;
namespace Rowles.LeanLucene.Codecs.Hnsw;

/// <summary>
/// Per-call HNSW search statistics. <see cref="NodesVisited"/> is the primary recall-vs-cost
/// signal: it counts distinct nodes visited during the layer-zero traversal.
/// </summary>
internal readonly record struct HnswSearchStats(int NodesVisited, int LayersDescended);
