using Rowles.LeanLucene.Codecs;
using Rowles.LeanLucene.Codecs.Hnsw;
using Rowles.LeanLucene.Codecs.Fst;
using Rowles.LeanLucene.Codecs.Bkd;
using Rowles.LeanLucene.Codecs.Vectors;
using Rowles.LeanLucene.Codecs.TermVectors.TermVectors;
using Rowles.LeanLucene.Codecs.TermDictionary;
namespace Rowles.LeanLucene.Codecs.Vectors;

/// <summary>
/// In-memory <see cref="IVectorSource"/> backed by an indexed dictionary of vectors.
/// Used during HNSW build before the .vec file has been written.
/// </summary>
internal sealed class InMemoryVectorSource : IVectorSource
{
    private readonly Dictionary<int, ReadOnlyMemory<float>> _vectors;

    public InMemoryVectorSource(Dictionary<int, ReadOnlyMemory<float>> vectors, int dimension)
    {
        ArgumentNullException.ThrowIfNull(vectors);
        _vectors = vectors;
        Dimension = dimension;
    }

    public int Dimension { get; }

    public int Count => _vectors.Count;

    public ReadOnlySpan<float> GetVector(int docId)
    {
        if (!_vectors.TryGetValue(docId, out var vec))
            throw new KeyNotFoundException($"No vector buffered for docId {docId}.");
        return vec.Span;
    }
}
