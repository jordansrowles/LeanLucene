namespace Rowles.LeanLucene.Document.Fields;

/// <summary>Full-text field passed through the analyser pipeline.</summary>
public sealed class TextField : IField
{
    /// <summary>
    /// Initialises a new <see cref="TextField"/> with the specified name and text value.
    /// </summary>
    /// <param name="name">The field name. Must not be null.</param>
    /// <param name="value">The text content to analyse and index. Must not be null.</param>
    public TextField(string name, string value)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Value = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <inheritdoc/>
    public string Name { get; }

    /// <summary>Gets the text content of this field.</summary>
    public string Value { get; }

    /// <inheritdoc/>
    public FieldType FieldType => FieldType.Text;

    /// <inheritdoc/>
    public bool IsStored => true;

    /// <inheritdoc/>
    public bool IsIndexed => true;
}
