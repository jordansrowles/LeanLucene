using System.Buffers;
using System.Runtime.CompilerServices;

namespace Rowles.LeanLucene.Store;

/// <summary>
/// Buffered sequential writer backed by <see cref="FileStream"/> and
/// <see cref="ArrayPool{T}"/>. Used at index-build time only.
/// </summary>
public sealed class IndexOutput : IDisposable
{
    private const int BufferSize = 65536;

    private readonly FileStream _stream;
    private readonly byte[] _buffer;
    private int _bufferPosition;
    private bool _disposed;

    /// <summary>Current logical write position (buffered + flushed).</summary>
    public long Position => _stream.Position + _bufferPosition;

    public IndexOutput(string filePath)
    {
        _stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
        _buffer = ArrayPool<byte>.Shared.Rent(BufferSize);
        _bufferPosition = 0;
    }

    /// <summary>
    /// Seeks to an absolute position. Flushes the buffer first to ensure consistency.
    /// Use sparingly — this forces a buffer flush.
    /// </summary>
    public void Seek(long position)
    {
        FlushBuffer();
        _stream.Seek(position, SeekOrigin.Begin);
    }

    public void WriteBytes(ReadOnlySpan<byte> data)
    {
        int offset = 0;
        while (offset < data.Length)
        {
            int remaining = BufferSize - _bufferPosition;
            int toCopy = Math.Min(remaining, data.Length - offset);
            data.Slice(offset, toCopy).CopyTo(_buffer.AsSpan(_bufferPosition, toCopy));
            _bufferPosition += toCopy;
            offset += toCopy;

            if (_bufferPosition == BufferSize)
                FlushBuffer();
        }
    }

    public void WriteBytes(byte[] data) => WriteBytes(data.AsSpan());

    public void WriteInt32(int value)
    {
        Span<byte> tmp = stackalloc byte[sizeof(int)];
        Unsafe.WriteUnaligned(ref tmp[0], value);
        WriteBytes(tmp);
    }

    public void WriteInt64(long value)
    {
        Span<byte> tmp = stackalloc byte[sizeof(long)];
        Unsafe.WriteUnaligned(ref tmp[0], value);
        WriteBytes(tmp);
    }

    public void WriteSingle(float value)
    {
        Span<byte> tmp = stackalloc byte[sizeof(float)];
        Unsafe.WriteUnaligned(ref tmp[0], value);
        WriteBytes(tmp);
    }

    public void WriteBoolean(bool value) => WriteByte(value ? (byte)1 : (byte)0);

    public void WriteByte(byte value)
    {
        if (_bufferPosition == BufferSize)
            FlushBuffer();

        _buffer[_bufferPosition++] = value;
    }

    /// <summary>
    /// Writes a non-negative integer using variable-length encoding (LEB128).
    /// Small values (0–127) consume a single byte.
    /// </summary>
    public void WriteVarInt(int value)
    {
        uint v = (uint)value;
        while (v >= 0x80)
        {
            WriteByte((byte)(v | 0x80));
            v >>= 7;
        }
        WriteByte((byte)v);
    }

    public void Flush() => FlushBuffer();

    private void FlushBuffer()
    {
        if (_bufferPosition > 0)
        {
            _stream.Write(_buffer, 0, _bufferPosition);
            _bufferPosition = 0;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        try
        {
            FlushBuffer();
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(_buffer);
            _stream.Dispose();
        }
    }
}
