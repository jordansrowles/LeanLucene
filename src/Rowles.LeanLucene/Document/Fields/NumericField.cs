namespace Rowles.LeanLucene.Document.Fields;

/// <summary>Numeric field for range filters and sorting.</summary>
public sealed class NumericField : IField
{
    public NumericField(string name, double value)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Value = value;
    }

    public string Name { get; }
    public double Value { get; }
    public FieldType FieldType => FieldType.Numeric;
    public bool IsStored => true;
    public bool IsIndexed => true;
}
