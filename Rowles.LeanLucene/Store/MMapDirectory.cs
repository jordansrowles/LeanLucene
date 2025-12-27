namespace Rowles.LeanLucene.Store;

/// <summary>
/// Primary directory implementation using memory-mapped files for reads
/// and buffered file streams for writes.
/// </summary>
public sealed class MMapDirectory : LeanDirectory
{
    public override string DirectoryPath { get; }

    public MMapDirectory(string path)
    {
        DirectoryPath = path ?? throw new ArgumentNullException(nameof(path));

        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
    }

    public override IndexOutput CreateOutput(string fileName)
    {
        var filePath = Path.Combine(DirectoryPath, ValidateFileName(fileName));
        return new IndexOutput(filePath);
    }

    public override IndexInput OpenInput(string fileName)
    {
        var filePath = Path.Combine(DirectoryPath, ValidateFileName(fileName));
        return new IndexInput(filePath);
    }

    public override void DeleteFile(string fileName)
    {
        var filePath = Path.Combine(DirectoryPath, ValidateFileName(fileName));
        File.Delete(filePath);
    }

    public override bool FileExists(string fileName)
    {
        var filePath = Path.Combine(DirectoryPath, ValidateFileName(fileName));
        return File.Exists(filePath);
    }

    public override string[] ListAll()
    {
        return Directory.GetFiles(DirectoryPath)
            .Select(Path.GetFileName)
            .Where(name => name is not null)
            .ToArray()!;
    }

    private static string ValidateFileName(string fileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        if (Path.IsPathRooted(fileName) || fileName != Path.GetFileName(fileName))
            throw new ArgumentException("File name must not contain path components.", nameof(fileName));

        return fileName;
    }
}
