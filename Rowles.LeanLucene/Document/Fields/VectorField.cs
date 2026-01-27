namespace Rowles.LeanLucene.Document.Fields;

/// <summary>Dense float vector for semantic and RAG workloads.</summary>
public sealed class VectorField : IField
{
    public VectorField(string name, ReadOnlyMemory<float> value)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Value = value;
    }

    public string Name { get; }
    public ReadOnlyMemory<float> Value { get; }
    public FieldType FieldType => FieldType.Vector;
    public bool IsStored => true;
    public bool IsIndexed => false;
}
