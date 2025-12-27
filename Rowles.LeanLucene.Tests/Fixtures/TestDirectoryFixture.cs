namespace Rowles.LeanLucene.Tests.Fixtures;

/// <summary>
/// Shared fixture that provisions a temporary directory for each test class
/// and tears it down afterwards.
/// </summary>
public sealed class TestDirectoryFixture : IDisposable
{
    public string Path { get; }

    public TestDirectoryFixture()
    {
        Path = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            "LeanLucene_Tests",
            Guid.NewGuid().ToString("N"));
        System.IO.Directory.CreateDirectory(Path);
    }

    public void Dispose()
    {
        if (System.IO.Directory.Exists(Path))
        {
            System.IO.Directory.Delete(Path, recursive: true);
        }
    }
}
