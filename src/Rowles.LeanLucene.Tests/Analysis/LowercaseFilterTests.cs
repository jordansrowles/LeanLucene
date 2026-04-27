using Rowles.LeanLucene.Analysis;
using Rowles.LeanLucene.Analysis.Analysers;

namespace Rowles.LeanLucene.Tests.Analysis;

[Trait("Category", "Analysis")]
public class LowercaseFilterTests
{
    private readonly LowercaseFilter _filter = new();

    [Fact]
    public void Apply_MixedCaseInput_LowercasesInPlace()
    {
        var buffer = "Hello WORLD FoO".ToCharArray();

        _filter.Apply(buffer);

        Assert.Equal("hello world foo", new string(buffer));
    }

    [Fact]
    public void Apply_AlreadyLowercase_RemainsUnchanged()
    {
        var buffer = "abc".ToCharArray();

        _filter.Apply(buffer);

        Assert.Equal("abc", new string(buffer));
    }

    [Fact]
    public void Apply_EmptyBuffer_DoesNotThrow()
    {
        _filter.Apply(Span<char>.Empty);
    }
}
