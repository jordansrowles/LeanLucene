using Rowles.LeanLucene.Search;

namespace Rowles.LeanLucene.Tests.Search;

public class CollectorTests
{
    [Fact]
    public void CountCollector_TracksHitCount()
    {
        // Arrange
        var collector = new CountCollector();

        // Act
        collector.Collect(0, 1.5f);
        collector.Collect(1, 2.0f);
        collector.Collect(2, 0.5f);

        // Assert
        Assert.Equal(3, collector.TotalHits);
    }

    [Fact]
    public void TopNCollectorWrapper_CollectsAndReturnsSorted()
    {
        // Arrange
        var collector = new TopNCollectorWrapper(2);

        // Act
        collector.Collect(0, 1.0f);
        collector.Collect(1, 3.0f);
        collector.Collect(2, 2.0f);

        var topDocs = collector.ToTopDocs();

        // Assert — top 2 by score
        Assert.Equal(3, collector.TotalHits);
        Assert.Equal(2, topDocs.ScoreDocs.Length);
        Assert.Equal(3.0f, topDocs.ScoreDocs[0].Score);
        Assert.Equal(2.0f, topDocs.ScoreDocs[1].Score);
    }

    [Fact]
    public void CountCollector_ZeroDocs_ReturnsZero()
    {
        var collector = new CountCollector();
        Assert.Equal(0, collector.TotalHits);
    }
}
