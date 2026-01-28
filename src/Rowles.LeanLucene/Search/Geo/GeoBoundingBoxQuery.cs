namespace Rowles.LeanLucene.Search.Geo;

/// <summary>
/// Query matching documents within a geographic bounding box.
/// </summary>
public sealed class GeoBoundingBoxQuery : Query
{
    public GeoBoundingBoxQuery(string field, double minLat, double maxLat, double minLon, double maxLon)
    {
        Field = field ?? throw new ArgumentNullException(nameof(field));
        MinLat = minLat; MaxLat = maxLat;
        MinLon = minLon; MaxLon = maxLon;
    }

    public override string Field { get; }
    public double MinLat { get; }
    public double MaxLat { get; }
    public double MinLon { get; }
    public double MaxLon { get; }

    public override bool Equals(object? obj)
        => obj is GeoBoundingBoxQuery q &&
           q.Field == Field && q.MinLat == MinLat && q.MaxLat == MaxLat &&
           q.MinLon == MinLon && q.MaxLon == MaxLon;

    public override int GetHashCode()
        => CombineBoost(HashCode.Combine(nameof(GeoBoundingBoxQuery), Field, MinLat, MaxLat, MinLon, MaxLon));
}