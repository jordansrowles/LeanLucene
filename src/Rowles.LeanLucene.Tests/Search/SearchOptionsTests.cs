using Rowles.LeanLucene.Search;

namespace Rowles.LeanLucene.Tests.Search;

public sealed class SearchOptionsTests
{
    [Fact]
    public void Default_HasNoLimits()
    {
        var options = SearchOptions.Default;

        Assert.Equal(long.MaxValue, options.MaxResultBytes);
        Assert.False(options.StreamResults);
        Assert.Null(options.Timeout);
    }

    [Fact]
    public void WithBudget_SetsMaxResultBytes()
    {
        var options = SearchOptions.WithBudget(1024);

        Assert.Equal(1024, options.MaxResultBytes);
    }

    [Fact]
    public void WithTimeout_SetsTimeout()
    {
        var timeout = TimeSpan.FromSeconds(5);

        var options = SearchOptions.WithTimeout(timeout);

        Assert.Equal(timeout, options.Timeout);
    }

    [Fact]
    public void Init_AllowsCustomCombination()
    {
        var options = new SearchOptions
        {
            MaxResultBytes = 1024,
            StreamResults = true,
            Timeout = TimeSpan.FromSeconds(1)
        };

        Assert.Equal(1024, options.MaxResultBytes);
        Assert.True(options.StreamResults);
        Assert.Equal(TimeSpan.FromSeconds(1), options.Timeout);
    }

    [Fact]
    public void Default_IsSingleton()
    {
        var first = SearchOptions.Default;
        var second = SearchOptions.Default;

        Assert.True(ReferenceEquals(first, second));
    }
}
