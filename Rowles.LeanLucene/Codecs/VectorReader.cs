namespace Rowles.LeanLucene.Codecs;

/// <summary>
/// Reads dense float vectors written by <see cref="VectorWriter"/>.
/// Uses the fixed dimension to compute offsets for direct seeking.
/// </summary>
public sealed class VectorReader : IDisposable
{
    private readonly FileStream _fs;
    private readonly BinaryReader _reader;
    private readonly int _vectorCount;
    private readonly int _dimension;
    private readonly long _dataStart;
    private bool _disposed;

    private VectorReader(FileStream fs, BinaryReader reader, int vectorCount, int dimension, long dataStart)
    {
        _fs = fs;
        _reader = reader;
        _vectorCount = vectorCount;
        _dimension = dimension;
        _dataStart = dataStart;
    }

    public static VectorReader Open(string filePath)
    {
        var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var reader = new BinaryReader(fs, System.Text.Encoding.UTF8, leaveOpen: true);

        int vectorCount = reader.ReadInt32();
        int dimension = reader.ReadInt32();
        long dataStart = fs.Position;

        return new VectorReader(fs, reader, vectorCount, dimension, dataStart);
    }

    public float[] ReadVector(int docId)
    {
        long offset = _dataStart + (long)docId * _dimension * sizeof(float);
        _fs.Seek(offset, SeekOrigin.Begin);

        var vector = new float[_dimension];
        for (int i = 0; i < _dimension; i++)
            vector[i] = _reader.ReadSingle();

        return vector;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _reader.Dispose();
        _fs.Dispose();
    }
}
