namespace Rowles.LeanLucene.Search.Geo;

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