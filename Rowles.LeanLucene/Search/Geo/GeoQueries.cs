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

/// <summary>
/// Query matching documents within a specified distance from a centre point.
/// </summary>
public sealed class GeoDistanceQuery : Query
{
    public GeoDistanceQuery(string field, double centreLat, double centreLon, double radiusMetres)
    {
        Field = field ?? throw new ArgumentNullException(nameof(field));
        CentreLat = centreLat;
        CentreLon = centreLon;
        RadiusMetres = radiusMetres;
    }

    public override string Field { get; }
    public double CentreLat { get; }
    public double CentreLon { get; }
    public double RadiusMetres { get; }

    public override bool Equals(object? obj)
        => obj is GeoDistanceQuery q &&
           q.Field == Field && q.CentreLat == CentreLat &&
           q.CentreLon == CentreLon && q.RadiusMetres == RadiusMetres;

    public override int GetHashCode()
        => CombineBoost(HashCode.Combine(nameof(GeoDistanceQuery), Field, CentreLat, CentreLon, RadiusMetres));
}
