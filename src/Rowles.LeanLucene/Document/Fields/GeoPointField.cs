namespace Rowles.LeanLucene.Document.Fields;

/// <summary>
/// Indexed geo-point field. Stores latitude and longitude as two numeric fields
/// (fieldName_lat and fieldName_lon) for range filtering and distance queries.
/// </summary>
public sealed class GeoPointField : IField
{
    /// <summary>
    /// Initialises a new <see cref="GeoPointField"/> with the specified name, latitude, and longitude.
    /// </summary>
    /// <param name="name">The base field name. Latitude and longitude are stored as <c>name_lat</c> and <c>name_lon</c>. Must not be null.</param>
    /// <param name="latitude">The latitude in decimal degrees (−90 to +90).</param>
    /// <param name="longitude">The longitude in decimal degrees (−180 to +180).</param>
    public GeoPointField(string name, double latitude, double longitude)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Latitude = latitude;
        Longitude = longitude;
        Value = $"{latitude},{longitude}";
    }

    /// <inheritdoc/>
    public string Name { get; }

    /// <summary>Gets the latitude component of this geo-point in decimal degrees.</summary>
    public double Latitude { get; }

    /// <summary>Gets the longitude component of this geo-point in decimal degrees.</summary>
    public double Longitude { get; }

    /// <summary>Gets the serialised value of this field as "latitude,longitude".</summary>
    public string Value { get; }

    /// <inheritdoc/>
    public FieldType FieldType => FieldType.Numeric;

    /// <inheritdoc/>
    public bool IsStored => true;

    /// <inheritdoc/>
    public bool IsIndexed => true;

    /// <summary>Returns the lat sub-field name.</summary>
    public string LatFieldName => Name + "_lat";

    /// <summary>Returns the lon sub-field name.</summary>
    public string LonFieldName => Name + "_lon";
}
