using Rowles.LeanLucene.Document;
using Rowles.LeanLucene.Document.Fields;
using Rowles.LeanLucene.Index.Indexer;

namespace Rowles.LeanLucene.Tests.Index;

public sealed class SchemaValidationTests
{
    [Fact]
    public void Validate_RequiredFieldMissing_Throws()
    {
        var schema = new IndexSchema()
            .Add(new FieldMapping("title", FieldType.Text) { IsRequired = true });

        var doc = new LeanDocument();
        doc.Add(new NumericField("price", 9.99));

        var ex = Assert.Throws<SchemaValidationException>(() => schema.Validate(doc));
        Assert.Contains("title", ex.Message);
    }

    [Fact]
    public void Validate_WrongFieldType_Throws()
    {
        var schema = new IndexSchema()
            .Add(new FieldMapping("price", FieldType.Numeric));

        var doc = new LeanDocument();
        doc.Add(new TextField("price", "not a number"));

        var ex = Assert.Throws<SchemaValidationException>(() => schema.Validate(doc));
        Assert.Contains("price", ex.Message);
        Assert.Contains("Numeric", ex.Message);
    }

    [Fact]
    public void Validate_StrictMode_RejectsUnknownFields()
    {
        var schema = new IndexSchema { StrictMode = true }
            .Add(new FieldMapping("title", FieldType.Text));

        var doc = new LeanDocument();
        doc.Add(new TextField("title", "hello"));
        doc.Add(new TextField("body", "world")); // not in schema

        var ex = Assert.Throws<SchemaValidationException>(() => schema.Validate(doc));
        Assert.Contains("body", ex.Message);
    }

    [Fact]
    public void Validate_NonStrictMode_AllowsUnknownFields()
    {
        var schema = new IndexSchema { StrictMode = false }
            .Add(new FieldMapping("title", FieldType.Text));

        var doc = new LeanDocument();
        doc.Add(new TextField("title", "hello"));
        doc.Add(new TextField("body", "world"));

        // Should not throw
        schema.Validate(doc);
    }

    [Fact]
    public void Validate_CorrectDocument_Passes()
    {
        var schema = new IndexSchema()
            .Add(new FieldMapping("title", FieldType.Text) { IsRequired = true })
            .Add(new FieldMapping("price", FieldType.Numeric));

        var doc = new LeanDocument();
        doc.Add(new TextField("title", "Widget"));
        doc.Add(new NumericField("price", 19.99));

        schema.Validate(doc); // no exception
    }
}
