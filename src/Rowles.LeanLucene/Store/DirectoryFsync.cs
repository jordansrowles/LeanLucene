using System.Buffers;
using System.Runtime.InteropServices;
using System.Text;

namespace Rowles.LeanLucene.Store;

/// <summary>
/// Cross-platform helper that flushes a directory's metadata (file-entry renames, creations,
/// deletions) to durable storage. Required for crash-safe atomic-rename commit protocols on
/// POSIX filesystems where directory entries are buffered independently of file contents.
/// </summary>
/// <remarks>
/// On Linux and macOS this opens the directory read-only, calls <c>fsync</c>, and closes
/// the descriptor. On Windows this is a no-op: NTFS journals directory updates synchronously
/// as part of the metadata transaction log, so an explicit directory sync is unnecessary
/// (and the Win32 API has no equivalent to <c>fsync(directory_fd)</c>).
/// </remarks>
internal static class DirectoryFsync
{
    private const int O_RDONLY = 0;

    /// <summary>
    /// Forces the directory's metadata to be persisted to the underlying storage device.
    /// On Windows this is a no-op. On Unix, errors are swallowed: directory sync is best-effort
    /// (some filesystems and exotic mounts do not support it; the surrounding rename remains
    /// atomic-by-name even without it).
    /// </summary>
    /// <param name="directoryPath">The absolute path of the directory to flush.</param>
    public static void Sync(string directoryPath)
    {
        if (string.IsNullOrEmpty(directoryPath)) return;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;

        int byteCount = Encoding.UTF8.GetByteCount(directoryPath);
        // +1 for null terminator. Use a rented buffer to avoid string interpolation / Marshal alloc.
        byte[] rented = ArrayPool<byte>.Shared.Rent(byteCount + 1);
        try
        {
            int written = Encoding.UTF8.GetBytes(directoryPath, 0, directoryPath.Length, rented, 0);
            rented[written] = 0;

            int fd;
            unsafe
            {
                fixed (byte* ptr = rented)
                {
                    fd = open(ptr, O_RDONLY);
                }
            }
            if (fd < 0) return;

            try { _ = fsync(fd); }
            finally { _ = close(fd); }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rented);
        }
    }

    [DllImport("libc", EntryPoint = "open", SetLastError = true)]
    private static extern unsafe int open(byte* pathname, int flags);

    [DllImport("libc", EntryPoint = "fsync", SetLastError = true)]
    private static extern int fsync(int fd);

    [DllImport("libc", EntryPoint = "close", SetLastError = true)]
    private static extern int close(int fd);

    /// <summary>
    /// Forces a previously written file's contents to be persisted to the underlying storage
    /// device. Equivalent to <c>fsync</c> on Unix and <c>FlushFileBuffers</c> on Windows.
    /// Errors are swallowed (best-effort).
    /// </summary>
    public static void SyncFile(string filePath)
    {
        if (string.IsNullOrEmpty(filePath)) return;
        try
        {
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Write, FileShare.ReadWrite);
            fs.Flush(flushToDisk: true);
        }
        catch (FileNotFoundException) { }
        catch (DirectoryNotFoundException) { }
        catch (UnauthorizedAccessException) { }
        catch (IOException) { }
    }
}
