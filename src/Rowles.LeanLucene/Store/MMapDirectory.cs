namespace Rowles.LeanLucene.Store;

/// <summary>
/// Primary directory implementation using memory-mapped files for reads
/// and buffered file streams for writes.
/// </summary>
public sealed class MMapDirectory : LeanDirectory
{
    /// <inheritdoc/>
    public override string DirectoryPath { get; }

    /// <summary>
    /// Initialises a new <see cref="MMapDirectory"/> backed by the given file system path.
    /// Creates the directory if it does not already exist.
    /// </summary>
    /// <param name="path">The file system path for the index directory. Must not be null.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="path"/> is null.</exception>
    public MMapDirectory(string path)
    {
        DirectoryPath = path ?? throw new ArgumentNullException(nameof(path));

        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
    }

    /// <inheritdoc/>
    public override IndexOutput CreateOutput(string fileName)
    {
        var filePath = Path.Combine(DirectoryPath, ValidateFileName(fileName));
        return new IndexOutput(filePath);
    }

    /// <inheritdoc/>
    public override IndexInput OpenInput(string fileName)
    {
        var filePath = Path.Combine(DirectoryPath, ValidateFileName(fileName));
        return new IndexInput(filePath);
    }

    /// <inheritdoc/>
    public override void DeleteFile(string fileName)
    {
        var filePath = Path.Combine(DirectoryPath, ValidateFileName(fileName));
        File.Delete(filePath);
    }

    /// <inheritdoc/>
    public override bool FileExists(string fileName)
    {
        var filePath = Path.Combine(DirectoryPath, ValidateFileName(fileName));
        return File.Exists(filePath);
    }

    /// <inheritdoc/>
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

        // Cross-platform: Path.GetFileName on POSIX treats backslash as a
        // regular character, so "..\\..\\foo" passes the check above. Reject
        // every separator and every traversal segment explicitly.
        if (fileName.Contains('/') || fileName.Contains('\\') || fileName.Contains(".."))
            throw new ArgumentException("File name must not contain path separators or traversal segments.", nameof(fileName));

        foreach (var c in fileName)
        {
            if (char.IsControl(c))
                throw new ArgumentException("File name must not contain control characters.", nameof(fileName));
        }

        return fileName;
    }
}
