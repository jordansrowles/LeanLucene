namespace Rowles.LeanLucene.Index.Indexer;

/// <summary>
/// Thrown when a document fails <see cref="IndexSchema"/> validation.
/// </summary>
public sealed class SchemaValidationException : InvalidOperationException
{
    public SchemaValidationException(string message) : base(message) { }
}
