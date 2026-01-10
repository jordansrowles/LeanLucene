using System.IO.MemoryMappedFiles;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Rowles.LeanLucene.Store;

/// <summary>
/// Readable input over a memory-mapped file. Maintains a position cursor
/// and uses <see cref="Unsafe.ReadUnaligned{T}"/> for primitive reads.
/// Acquired pointer is held for the lifetime of the accessor to avoid
/// repeated acquire/release overhead.
/// </summary>
public sealed unsafe class IndexInput : IDisposable
{
    private readonly MemoryMappedFile _mmf;
    private readonly MemoryMappedViewAccessor _accessor;
    private readonly long _length;
    private long _position;
    private bool _disposed;
    private byte* _ptr;

    public IndexInput(string filePath)
    {
        var fileInfo = new FileInfo(filePath);
        _length = fileInfo.Length;

        if (_length == 0)
        {
            // Empty file — no data to map. Allow reads to fail with EndOfStream.
            _mmf = MemoryMappedFile.CreateNew(null, 1, MemoryMappedFileAccess.Read);
            _accessor = _mmf.CreateViewAccessor(0, 1, MemoryMappedFileAccess.Read);
            _ptr = null;
            _accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref _ptr!);
            _ptr += _accessor.PointerOffset;
            return;
        }

        _mmf = MemoryMappedFile.CreateFromFile(filePath, FileMode.Open, null, 0, MemoryMappedFileAccess.Read);
        _accessor = _mmf.CreateViewAccessor(0, _length, MemoryMappedFileAccess.Read);
        _ptr = null;
        _accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref _ptr);
        _ptr += _accessor.PointerOffset;
    }

    /// <summary>Total file length in bytes.</summary>
    public long Length => _length;

    /// <summary>Current read position within the file.</summary>
    public long Position => _position;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Seek(long position)
    {
        _position = position;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte ReadByte()
    {
        if (_position >= _length)
            ThrowEndOfStream();
        byte value = _ptr[_position];
        _position++;
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ReadBoolean()
    {
        return ReadByte() != 0;
    }

    public byte[] ReadBytes(int count)
    {
        if (_position + count > _length)
            ThrowEndOfStream();
        var result = new byte[count];
        new ReadOnlySpan<byte>(_ptr + _position, count).CopyTo(result);
        _position += count;
        return result;
    }

    /// <summary>
    /// Returns a read-only span over the memory-mapped buffer at the current position
    /// without allocating. Advances the position by <paramref name="count"/> bytes.
    /// The span is only valid while the <see cref="IndexInput"/> is not disposed.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<byte> ReadSpan(int count)
    {
        if (_position + count > _length)
            ThrowEndOfStream();
        var span = new ReadOnlySpan<byte>(_ptr + _position, count);
        _position += count;
        return span;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ReadInt32()
    {
        if (_position + sizeof(int) > _length)
            ThrowEndOfStream();
        int value = Unsafe.ReadUnaligned<int>(_ptr + _position);
        _position += sizeof(int);
        return value;
    }

    /// <summary>
    /// Bulk-reads <paramref name="count"/> int32 values into the destination span.
    /// Single bounds check for the entire block. Much faster than N × ReadInt32().
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ReadInt32Array(Span<int> dest, int count)
    {
        int byteCount = count * sizeof(int);
        if (_position + byteCount > _length)
            ThrowEndOfStream();

        new ReadOnlySpan<byte>(_ptr + _position, byteCount)
            .CopyTo(MemoryMarshal.AsBytes(dest[..count]));
        _position += byteCount;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long ReadInt64()
    {
        if (_position + sizeof(long) > _length)
            ThrowEndOfStream();
        long value = Unsafe.ReadUnaligned<long>(_ptr + _position);
        _position += sizeof(long);
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float ReadSingle()
    {
        if (_position + sizeof(float) > _length)
            ThrowEndOfStream();
        float value = Unsafe.ReadUnaligned<float>(_ptr + _position);
        _position += sizeof(float);
        return value;
    }

    /// <summary>
    /// Reads <paramref name="charCount"/> chars encoded as UTF-8 (as written by BinaryWriter.Write(char[])).
    /// Returns a newly allocated string. Used for one-time skip index loading.
    /// </summary>
    public string ReadUtf8String(int charCount)
    {
        byte* start = _ptr + _position;
        int byteCount = Utf8ByteCount(start, charCount);
        if (_position + byteCount > _length)
            ThrowEndOfStream();

        Span<char> buf = charCount <= 256 ? stackalloc char[charCount] : new char[charCount];
        System.Text.Encoding.UTF8.GetChars(new ReadOnlySpan<byte>(start, byteCount), buf);
        _position += byteCount;
        return new string(buf);
    }

    /// <summary>
    /// Compares <paramref name="charCount"/> UTF-8-encoded chars at the current position
    /// against <paramref name="termUtf8"/> raw UTF-8 bytes. Advances position past the bytes.
    /// Zero-allocation, no char decoding needed.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareUtf8BytesAndAdvance(int charCount, ReadOnlySpan<byte> termUtf8)
    {
        byte* start = _ptr + _position;
        int byteCount = Utf8ByteCount(start, charCount);
        if (_position + byteCount > _length)
            ThrowEndOfStream();

        var fileBytes = new ReadOnlySpan<byte>(start, byteCount);
        _position += byteCount;
        return fileBytes.SequenceCompareTo(termUtf8);
    }

    /// <summary>
    /// Compares <paramref name="charCount"/> UTF-8-encoded chars at the current position
    /// against <paramref name="term"/> using ordinal comparison. Advances position past the bytes.
    /// Zero-allocation (stackalloc for decode buffer).
    /// </summary>
    public int CompareCharsAndAdvance(int charCount, ReadOnlySpan<char> term)
    {
        byte* start = _ptr + _position;
        int byteCount = Utf8ByteCount(start, charCount);
        if (_position + byteCount > _length)
            ThrowEndOfStream();

        Span<char> buf = charCount <= 256 ? stackalloc char[charCount] : new char[charCount];
        System.Text.Encoding.UTF8.GetChars(new ReadOnlySpan<byte>(start, byteCount), buf);
        _position += byteCount;
        return buf.SequenceCompareTo(term);
    }

    /// <summary>Counts the number of UTF-8 bytes needed to encode <paramref name="charCount"/> characters.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int Utf8ByteCount(byte* p, int charCount)
    {
        // Fast path: check if the first charCount bytes are all ASCII
        // For ASCII-only text, byte count == char count
        bool allAscii = true;
        for (int i = 0; i < charCount; i++)
        {
            if (p[i] >= 0x80) { allAscii = false; break; }
        }
        if (allAscii) return charCount;

        // Slow path: variable-width UTF-8
        int bytes = 0;
        int chars = 0;
        while (chars < charCount)
        {
            byte b = p[bytes];
            if (b < 0x80) { bytes += 1; chars += 1; }
            else if ((b & 0xE0) == 0xC0) { bytes += 2; chars += 1; }
            else if ((b & 0xF0) == 0xE0) { bytes += 3; chars += 1; }
            else { bytes += 4; chars += 2; } // 4-byte UTF-8 = surrogate pair (2 UTF-16 chars)
        }
        return bytes;
    }

    /// <summary>
    /// Reads a variable-length encoded non-negative integer (LEB128).
    /// Small values (0–127) consume a single byte.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ReadVarInt()
    {
        uint result = 0;
        int shift = 0;
        byte b;
        do
        {
            if (_position >= _length)
                ThrowEndOfStream();
            b = _ptr[_position++];
            if (shift >= 35)
                throw new InvalidDataException("VarInt is too large or malformed.");
            result |= (uint)(b & 0x7F) << shift;
            shift += 7;
        } while ((b & 0x80) != 0);

        if (result > int.MaxValue)
            throw new InvalidDataException("VarInt exceeds Int32 range.");
        return (int)result;
    }

    /// <summary>
    /// Hints the OS to prefetch the mapped region for sequential access.
    /// Uses PrefetchVirtualMemory on Windows and madvise(MADV_SEQUENTIAL) on Linux.
    /// Failures are silently ignored (advisory only).
    /// </summary>
    public void Prefetch()
    {
        if (_length == 0 || _ptr == null) return;

        if (OperatingSystem.IsWindows())
            PrefetchWindows();
        else if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
            PrefetchPosix();
    }

    private void PrefetchWindows()
    {
        nint handle = Environment.ProcessId != 0
            ? NativeMethods.GetCurrentProcess()
            : IntPtr.Zero;
        if (handle == IntPtr.Zero) return;

        var entry = new NativeMethods.WIN32_MEMORY_RANGE_ENTRY
        {
            VirtualAddress = (nint)_ptr,
            NumberOfBytes = (nuint)_length
        };
        NativeMethods.PrefetchVirtualMemory(handle, 1, &entry, 0);
    }

    private void PrefetchPosix()
    {
        // MADV_SEQUENTIAL = 2 on Linux and macOS
        NativeMethods.madvise((nint)_ptr, (nuint)_length, 2);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _accessor.SafeMemoryMappedViewHandle.ReleasePointer();
        _ptr = null;
        _accessor.Dispose();
        _mmf.Dispose();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowEndOfStream()
        => throw new EndOfStreamException("Attempted to read beyond the end of the mapped file.");
}
