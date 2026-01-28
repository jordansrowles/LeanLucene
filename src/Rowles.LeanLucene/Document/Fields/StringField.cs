namespace Rowles.LeanLucene.Document.Fields;

/// <summary>Exact-match field stored as-is, not passed through the analyser.</summary>
public sealed class StringField : IField
{
    public StringField(string name, string value)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Value = value ?? throw new ArgumentNullException(nameof(value));
    }

    public string Name { get; }
    public string Value { get; }
    public FieldType FieldType => FieldType.String;
    public bool IsStored => true;
    public bool IsIndexed => true;
}
