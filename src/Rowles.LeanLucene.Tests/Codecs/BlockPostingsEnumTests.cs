using Rowles.LeanLucene.Codecs.Postings;
using Rowles.LeanLucene.Store;

namespace Rowles.LeanLucene.Tests.Codecs;

public sealed class BlockPostingsEnumTests : IDisposable
{
    private readonly string _tempDir;

    public BlockPostingsEnumTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "leanlucene_bpe_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, true); } catch { }
    }

    private (string docPath, TermPostingMetadata meta) WritePostings(
        int[] docIds, int[] freqs)
    {
        var docPath = Path.Combine(_tempDir, $"test_{Guid.NewGuid():N}.doc");
        TermPostingMetadata meta;
        using (var docOut = new IndexOutput(docPath))
        {
            using var writer = new BlockPostingsWriter(docOut);
            writer.StartTerm();
            for (int i = 0; i < docIds.Length; i++)
                writer.AddPosting(docIds[i], freqs[i]);
            meta = writer.FinishTerm();
        }
        return (docPath, meta);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(127)]
    [InlineData(128)]
    [InlineData(129)]
    [InlineData(256)]
    [InlineData(1000)]
    public void NextDoc_ReturnsAllDocIds(int count)
    {
        var docIds = new int[count];
        var freqs = new int[count];
        for (int i = 0; i < count; i++)
        {
            docIds[i] = i * 3; // gaps of 3
            freqs[i] = 1;
        }

        var (docPath, meta) = WritePostings(docIds, freqs);

        using var input = new IndexInput(docPath);
        var pe = BlockPostingsEnum.Create(input, meta.DocStartOffset, meta.SkipOffset, meta.DocFreq);

        var result = new List<int>();
        int doc;
        while ((doc = pe.NextDoc()) != BlockPostingsEnum.NoMoreDocs)
            result.Add(doc);

        Assert.Equal(docIds, result.ToArray());
    }

    [Theory]
    [InlineData(1)]
    [InlineData(128)]
    [InlineData(200)]
    public void Freq_RoundTrips(int count)
    {
        var docIds = new int[count];
        var freqs = new int[count];
        for (int i = 0; i < count; i++)
        {
            docIds[i] = i;
            freqs[i] = (i % 5) + 1; // 1..5
        }

        var (docPath, meta) = WritePostings(docIds, freqs);

        using var input = new IndexInput(docPath);
        var pe = BlockPostingsEnum.Create(input, meta.DocStartOffset, meta.SkipOffset, meta.DocFreq);

        var resultFreqs = new List<int>();
        while (pe.NextDoc() != BlockPostingsEnum.NoMoreDocs)
            resultFreqs.Add(pe.Freq);

        Assert.Equal(freqs, resultFreqs.ToArray());
    }

    [Fact]
    public void Advance_SkipsToTarget()
    {
        int count = 500;
        var docIds = new int[count];
        var freqs = new int[count];
        for (int i = 0; i < count; i++)
        {
            docIds[i] = i * 2; // even: 0, 2, 4, ..., 998
            freqs[i] = 1;
        }

        var (docPath, meta) = WritePostings(docIds, freqs);

        using var input = new IndexInput(docPath);
        var pe = BlockPostingsEnum.Create(input, meta.DocStartOffset, meta.SkipOffset, meta.DocFreq);

        // Advance to doc 400 (exists at index 200)
        int doc = pe.Advance(400);
        Assert.Equal(400, doc);

        // Advance to doc 801 → should land on 802
        doc = pe.Advance(801);
        Assert.Equal(802, doc);

        // Advance past all → NoMoreDocs
        doc = pe.Advance(9999);
        Assert.Equal(BlockPostingsEnum.NoMoreDocs, doc);
    }

    [Fact]
    public void Advance_WithinCurrentBlock()
    {
        int count = 128; // exactly one block
        var docIds = new int[count];
        var freqs = new int[count];
        for (int i = 0; i < count; i++)
        {
            docIds[i] = i * 10;
            freqs[i] = 1;
        }

        var (docPath, meta) = WritePostings(docIds, freqs);

        using var input = new IndexInput(docPath);
        var pe = BlockPostingsEnum.Create(input, meta.DocStartOffset, meta.SkipOffset, meta.DocFreq);

        // Move into the block first
        pe.NextDoc(); // doc 0
        Assert.Equal(0, pe.DocId);

        // Advance within the same block
        int doc = pe.Advance(500);
        Assert.Equal(500, doc);

        doc = pe.Advance(1270);
        Assert.Equal(1270, doc);
    }

    [Fact]
    public void Advance_AcrossMultipleBlocks()
    {
        int count = 10_000;
        var docIds = new int[count];
        var freqs = new int[count];
        for (int i = 0; i < count; i++)
        {
            docIds[i] = i;
            freqs[i] = 1;
        }

        var (docPath, meta) = WritePostings(docIds, freqs);

        using var input = new IndexInput(docPath);
        var pe = BlockPostingsEnum.Create(input, meta.DocStartOffset, meta.SkipOffset, meta.DocFreq);

        // Jump to different blocks
        Assert.Equal(5000, pe.Advance(5000));
        Assert.Equal(7777, pe.Advance(7777));
        Assert.Equal(9999, pe.Advance(9999));
        Assert.Equal(BlockPostingsEnum.NoMoreDocs, pe.Advance(10000));
    }

    [Fact]
    public void EmptyPostings_ImmediatelyExhausted()
    {
        // Create a posting with 0 docs (write header only)
        var docPath = Path.Combine(_tempDir, "empty.doc");
        TermPostingMetadata meta;
        using (var docOut = new IndexOutput(docPath))
        {
            using var writer = new BlockPostingsWriter(docOut);
            writer.StartTerm();
            meta = writer.FinishTerm();
        }

        using var input = new IndexInput(docPath);
        var pe = BlockPostingsEnum.Create(input, meta.DocStartOffset, meta.SkipOffset, meta.DocFreq);

        Assert.Equal(BlockPostingsEnum.NoMoreDocs, pe.NextDoc());
        Assert.True(pe.IsExhausted);
    }

    [Fact]
    public void LargeScale_100K_AllDocsReturned()
    {
        int count = 100_000;
        var docIds = new int[count];
        var freqs = new int[count];
        for (int i = 0; i < count; i++)
        {
            docIds[i] = i * 2;
            freqs[i] = 1;
        }

        var (docPath, meta) = WritePostings(docIds, freqs);

        using var input = new IndexInput(docPath);
        var pe = BlockPostingsEnum.Create(input, meta.DocStartOffset, meta.SkipOffset, meta.DocFreq);

        int resultCount = 0;
        int lastDoc = -1;
        int doc;
        while ((doc = pe.NextDoc()) != BlockPostingsEnum.NoMoreDocs)
        {
            Assert.True(doc > lastDoc, "Doc IDs must be strictly increasing");
            Assert.Equal(docIds[resultCount], doc);
            lastDoc = doc;
            resultCount++;
        }

        Assert.Equal(count, resultCount);
    }

    [Fact]
    public void Advance_ToExactBlockBoundary()
    {
        // 256 docs = exactly 2 blocks, advance to first doc of second block
        int count = 256;
        var docIds = new int[count];
        var freqs = new int[count];
        for (int i = 0; i < count; i++)
        {
            docIds[i] = i;
            freqs[i] = 1;
        }

        var (docPath, meta) = WritePostings(docIds, freqs);

        using var input = new IndexInput(docPath);
        var pe = BlockPostingsEnum.Create(input, meta.DocStartOffset, meta.SkipOffset, meta.DocFreq);

        // Advance to doc 128 (first doc of second block)
        int doc = pe.Advance(128);
        Assert.Equal(128, doc);
        Assert.Equal(1, pe.Freq);
    }

    [Fact]
    public void Singleton_DocFreqOne_RoundTrips()
    {
        var (docPath, meta) = WritePostings([42], [3]);

        Assert.Equal(1, meta.DocFreq);
        Assert.Equal(42, meta.SingletonDocId);

        using var input = new IndexInput(docPath);
        var pe = BlockPostingsEnum.Create(input, meta.DocStartOffset, meta.SkipOffset, meta.DocFreq);

        Assert.Equal(42, pe.NextDoc());
        Assert.Equal(3, pe.Freq);
        Assert.Equal(BlockPostingsEnum.NoMoreDocs, pe.NextDoc());
    }

    [Fact]
    public void MixedFrequencies_LargeValues()
    {
        int count = 300;
        var docIds = new int[count];
        var freqs = new int[count];
        for (int i = 0; i < count; i++)
        {
            docIds[i] = i * 100;
            freqs[i] = (i % 2 == 0) ? 1 : 1000; // alternating 1 and 1000
        }

        var (docPath, meta) = WritePostings(docIds, freqs);

        using var input = new IndexInput(docPath);
        var pe = BlockPostingsEnum.Create(input, meta.DocStartOffset, meta.SkipOffset, meta.DocFreq);

        for (int i = 0; i < count; i++)
        {
            Assert.NotEqual(BlockPostingsEnum.NoMoreDocs, pe.NextDoc());
            Assert.Equal(docIds[i], pe.DocId);
            Assert.Equal(freqs[i], pe.Freq);
        }
        Assert.Equal(BlockPostingsEnum.NoMoreDocs, pe.NextDoc());
    }
}
