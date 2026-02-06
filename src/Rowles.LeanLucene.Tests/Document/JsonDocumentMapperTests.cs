using System.Text.Json;
using Rowles.LeanLucene.Document.Fields;
using Rowles.LeanLucene.Document.Json;

namespace Rowles.LeanLucene.Tests.Document;

public sealed class JsonDocumentMapperTests
{
    [Fact]
    public void FlatObject_MapsStringAndNumericFields()
    {
        var json = """{"name": "Alice", "age": 30}""";
        var doc = JsonDocumentMapper.FromJsonString(json);

        Assert.Equal(2, doc.Fields.Count);
        Assert.NotNull(doc.GetField("name"));
        Assert.NotNull(doc.GetField("age"));
        Assert.IsType<StringField>(doc.GetField("name"));
        Assert.IsType<NumericField>(doc.GetField("age"));
    }

    [Fact]
    public void NestedObject_ProducesPrefixedFieldNames()
    {
        var json = """{"address": {"city": "London", "zip": "SW1A"}}""";
        var doc = JsonDocumentMapper.FromJsonString(json);

        Assert.NotNull(doc.GetField("address.city"));
        Assert.NotNull(doc.GetField("address.zip"));
    }

    [Fact]
    public void Array_ProducesMultiValuedFields()
    {
        var json = """{"tags": ["red", "blue", "green"]}""";
        var doc = JsonDocumentMapper.FromJsonString(json);

        var fields = doc.GetFields("tags");
        Assert.Equal(3, fields.Count);
    }

    [Fact]
    public void BooleanValues_MappedAsStringFields()
    {
        var json = """{"active": true, "deleted": false}""";
        var doc = JsonDocumentMapper.FromJsonString(json);

        var active = doc.GetField("active") as StringField;
        var deleted = doc.GetField("deleted") as StringField;
        Assert.NotNull(active);
        Assert.NotNull(deleted);
        Assert.Equal("true", active!.Value);
        Assert.Equal("false", deleted!.Value);
    }

    [Fact]
    public void NullValues_AreSkipped()
    {
        var json = """{"name": "Alice", "bio": null}""";
        var doc = JsonDocumentMapper.FromJsonString(json);

        Assert.Null(doc.GetField("bio"));
        Assert.NotNull(doc.GetField("name"));
    }

    [Fact]
    public void LongStrings_BecomeTextFields()
    {
        var longText = new string('x', 100);
        var json = $$"""{"body": "{{longText}}"}""";
        var doc = JsonDocumentMapper.FromJsonString(json);

        Assert.IsType<TextField>(doc.GetField("body"));
    }

    [Fact]
    public void CustomSeparator_UsedForNestedNames()
    {
        var json = """{"address": {"city": "London"}}""";
        var opts = new JsonMappingOptions { FieldNameSeparator = "/" };
        var doc = JsonDocumentMapper.FromJsonString(json, opts);

        Assert.NotNull(doc.GetField("address/city"));
    }

    [Fact]
    public void EmptyObject_ReturnsEmptyDocument()
    {
        var doc = JsonDocumentMapper.FromJsonString("{}");
        Assert.Empty(doc.Fields);
    }
}
