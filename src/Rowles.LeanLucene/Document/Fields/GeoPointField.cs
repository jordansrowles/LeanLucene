namespace Rowles.LeanLucene.Document.Fields;

/// <summary>
/// Indexed geo-point field. Stores latitude and longitude as two numeric fields
/// (fieldName_lat and fieldName_lon) for range filtering and distance queries.
/// </summary>
public sealed class GeoPointField : IField
{
    public GeoPointField(string name, double latitude, double longitude)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Latitude = latitude;
        Longitude = longitude;
        Value = $"{latitude},{longitude}";
    }

    public string Name { get; }
    public double Latitude { get; }
    public double Longitude { get; }
    public string Value { get; }
    public FieldType FieldType => FieldType.Numeric;
    public bool IsStored => true;
    public bool IsIndexed => true;

    /// <summary>Returns the lat sub-field name.</summary>
    public string LatFieldName => Name + "_lat";

    /// <summary>Returns the lon sub-field name.</summary>
    public string LonFieldName => Name + "_lon";
}
