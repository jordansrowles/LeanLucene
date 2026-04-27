using Rowles.LeanLucene.Codecs;
using Rowles.LeanLucene.Codecs.Hnsw;
using Rowles.LeanLucene.Codecs.Fst;
using Rowles.LeanLucene.Codecs.Bkd;
using Rowles.LeanLucene.Codecs.Vectors;
using Rowles.LeanLucene.Codecs.TermVectors;
using Rowles.LeanLucene.Codecs.TermDictionary;
namespace Rowles.LeanLucene.Codecs.Hnsw;

/// <summary>
/// Build-time configuration for an <see cref="HnswGraph"/>.
/// </summary>
public sealed class HnswBuildConfig
{
    /// <summary>Maximum neighbours per node on layers above zero. Default 16.</summary>
    public int M { get; init; } = 16;

    /// <summary>Candidate set size during graph construction. Default 100.</summary>
    public int EfConstruction { get; init; } = 100;

    /// <summary>
    /// Maximum neighbours on layer zero. Defaults to <c>2 * M</c> when zero.
    /// </summary>
    public int M0 { get; init; }

    /// <summary>Effective layer-zero degree.</summary>
    internal int EffectiveM0 => M0 > 0 ? M0 : 2 * M;
}
