namespace Rowles.LeanLucene.Document.Fields;

/// <summary>Full-text field passed through the analyser pipeline.</summary>
public sealed class TextField : IField
{
    public TextField(string name, string value)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Value = value ?? throw new ArgumentNullException(nameof(value));
    }

    public string Name { get; }
    public string Value { get; }
    public FieldType FieldType => FieldType.Text;
    public bool IsStored => true;
    public bool IsIndexed => true;
}
