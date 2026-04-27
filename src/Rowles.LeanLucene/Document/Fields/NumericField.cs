namespace Rowles.LeanLucene.Document.Fields;

/// <summary>Numeric field for range filters and sorting.</summary>
public sealed class NumericField : IField
{
    /// <summary>
    /// Initialises a new <see cref="NumericField"/> with the specified name and numeric value.
    /// </summary>
    /// <param name="name">The field name. Must not be null.</param>
    /// <param name="value">The numeric value to index and store.</param>
    public NumericField(string name, double value)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Value = value;
    }

    /// <inheritdoc/>
    public string Name { get; }

    /// <summary>Gets the numeric value of this field.</summary>
    public double Value { get; }

    /// <inheritdoc/>
    public FieldType FieldType => FieldType.Numeric;

    /// <inheritdoc/>
    public bool IsStored => true;

    /// <inheritdoc/>
    public bool IsIndexed => true;
}
