using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;

namespace Rowles.LeanLucene.Codecs;

/// <summary>
/// Reads dense float vectors written by <see cref="VectorWriter"/>.
/// Uses memory-mapped I/O for zero-copy vector access.
/// </summary>
public sealed class VectorReader : IDisposable
{
    private readonly MemoryMappedFile _mmf;
    private readonly MemoryMappedViewAccessor _accessor;
    private readonly int _vectorCount;
    private readonly int _dimension;
    private readonly long _dataStart;
    private bool _disposed;

    private VectorReader(MemoryMappedFile mmf, MemoryMappedViewAccessor accessor, int vectorCount, int dimension, long dataStart)
    {
        _mmf = mmf;
        _accessor = accessor;
        _vectorCount = vectorCount;
        _dimension = dimension;
        _dataStart = dataStart;
    }

    public static VectorReader Open(string filePath)
    {
        var mmf = MemoryMappedFile.CreateFromFile(filePath, FileMode.Open, null, 0, MemoryMappedFileAccess.Read);
        var accessor = mmf.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);

        int vectorCount = accessor.ReadInt32(0);
        int dimension = accessor.ReadInt32(4);
        long dataStart = 8; // two int32s

        return new VectorReader(mmf, accessor, vectorCount, dimension, dataStart);
    }

    public float[] ReadVector(int docId)
    {
        long offset = _dataStart + (long)docId * _dimension * sizeof(float);
        var vector = new float[_dimension];
        _accessor.ReadArray(offset, vector, 0, _dimension);
        return vector;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _accessor.Dispose();
        _mmf.Dispose();
    }
}
