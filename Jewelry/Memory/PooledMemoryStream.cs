using System;
using System.Buffers;
using System.IO;
using System.Runtime.CompilerServices;

namespace Jewelry.Memory;

/// <summary>
/// A <see cref="Stream"/> implementation backed by a byte array rented from <see cref="ArrayPool{T}"/>.
/// Can be used as a drop-in replacement for <see cref="System.IO.MemoryStream"/>.
/// The rented buffer is returned to the pool on <see cref="Dispose"/>.
/// </summary>
/// <remarks>
/// This class is not thread-safe, matching the design of <see cref="System.IO.MemoryStream"/>.
/// </remarks>
public sealed class PooledMemoryStream : Stream
{
    // ─── Fields ──────────────────────────────────────────────────────────────────

    private byte[] _buffer; // Buffer rented from ArrayPool (Length >= _length)
    private int _length; // Logical size of the stream (bytes written)
    private int _position; // Current read/write position
    private bool _disposed;

    private readonly ArrayPool<byte> _pool;

    // ─── Constants ────────────────────────────────────────────────────────────────

    private const int DefaultInitialCapacity = 256;
    private const int MaxArrayLength = 0x7FFFFFC7; // Same as Array.MaxLength

    // ─── Constructors ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Initializes a new instance of <see cref="PooledMemoryStream"/> with default settings.
    /// </summary>
    public PooledMemoryStream()
        : this(DefaultInitialCapacity, ArrayPool<byte>.Shared)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="PooledMemoryStream"/> with the specified initial capacity.
    /// </summary>
    /// <param name="initialCapacity">The initial capacity in bytes. Must be zero or greater.</param>
    public PooledMemoryStream(int initialCapacity)
        : this(initialCapacity, ArrayPool<byte>.Shared)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="PooledMemoryStream"/> with the specified <see cref="ArrayPool{T}"/>.
    /// </summary>
    /// <param name="initialCapacity">The initial capacity in bytes. Must be zero or greater.</param>
    /// <param name="pool">The <see cref="ArrayPool{T}"/> to use for buffer allocation.</param>
    public PooledMemoryStream(int initialCapacity, ArrayPool<byte> pool)
    {
        if (initialCapacity < 0)
            throw new ArgumentOutOfRangeException(nameof(initialCapacity), initialCapacity,
                "initialCapacity must be zero or greater.");
        ArgumentNullException.ThrowIfNull(pool);

        _pool = pool;
        _buffer = initialCapacity == 0
            ? []
            : pool.Rent(initialCapacity);
        _length = 0;
        _position = 0;
    }

    // ─── Stream Properties ────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public override bool CanRead
    {
        get
        {
            ThrowIfDisposed();
            return true;
        }
    }

    /// <inheritdoc/>
    public override bool CanSeek
    {
        get
        {
            ThrowIfDisposed();
            return true;
        }
    }

    /// <inheritdoc/>
    public override bool CanWrite
    {
        get
        {
            ThrowIfDisposed();
            return true;
        }
    }

    /// <inheritdoc/>
    public override long Length
    {
        get
        {
            ThrowIfDisposed();
            return _length;
        }
    }

    /// <inheritdoc/>
    public override long Position
    {
        get
        {
            ThrowIfDisposed();
            return _position;
        }
        set
        {
            ThrowIfDisposed();
            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(value), value,
                    "Position must be zero or greater.");
            if (value > MaxArrayLength)
                throw new ArgumentOutOfRangeException(nameof(value), value,
                    "Position exceeds the maximum allowed value.");
            _position = (int)value;
        }
    }

    /// <summary>
    /// Gets or sets the current capacity (size of the internal buffer) in bytes.
    /// </summary>
    public int Capacity
    {
        get
        {
            ThrowIfDisposed();
            return _buffer.Length;
        }
        set
        {
            ThrowIfDisposed();
            if (value < _length)
                throw new ArgumentOutOfRangeException(nameof(value), value,
                    "Capacity must be greater than or equal to Length.");
            SetCapacity(value);
        }
    }

    // ─── Stream Methods ───────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public override void Flush()
    {
        ThrowIfDisposed();
        // No-op, same as MemoryStream
    }

    /// <inheritdoc/>
    public override int Read(byte[] buffer, int offset, int count)
    {
        ThrowIfDisposed();
        ValidateBufferArguments(buffer, offset, count);

        var remaining = _length - _position;
        if (remaining <= 0) return 0;

        var toRead = Math.Min(count, remaining);
        Buffer.BlockCopy(_buffer, _position, buffer, offset, toRead);
        _position += toRead;
        return toRead;
    }

    /// <inheritdoc/>
    public override int Read(Span<byte> buffer)
    {
        ThrowIfDisposed();

        var remaining = _length - _position;
        if (remaining <= 0) return 0;

        var toRead = Math.Min(buffer.Length, remaining);
        _buffer.AsSpan(_position, toRead).CopyTo(buffer);
        _position += toRead;
        return toRead;
    }

    /// <inheritdoc/>
    public override int ReadByte()
    {
        ThrowIfDisposed();

        if (_position >= _length) return -1;

        return _buffer[_position++];
    }

    /// <inheritdoc/>
    public override long Seek(long offset, SeekOrigin origin)
    {
        ThrowIfDisposed();

        long newPosition = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => _position + offset,
            SeekOrigin.End => _length + offset,
            _ => throw new ArgumentException($"Unknown SeekOrigin: {origin}", nameof(origin))
        };

        if (newPosition < 0)
            throw new IOException("Cannot seek before the beginning of the stream.");
        if (newPosition > MaxArrayLength)
            throw new ArgumentOutOfRangeException(nameof(offset), "Seek position exceeds the maximum allowed value.");

        _position = (int)newPosition;
        return _position;
    }

    /// <inheritdoc/>
    public override void SetLength(long value)
    {
        ThrowIfDisposed();

        if (value < 0)
            throw new ArgumentOutOfRangeException(nameof(value), value,
                "Length must be zero or greater.");
        if (value > MaxArrayLength)
            throw new ArgumentOutOfRangeException(nameof(value), value,
                "Length exceeds the maximum allowed value.");

        var newLength = (int)value;

        if (newLength > _buffer.Length)
            EnsureCapacity(newLength);

        if (newLength > _length)
        {
            // Zero-fill the expanded region, same as MemoryStream
            _buffer.AsSpan(_length, newLength - _length).Clear();
        }

        _length = newLength;

        if (_position > _length)
            _position = _length;
    }

    /// <inheritdoc/>
    public override void Write(byte[] buffer, int offset, int count)
    {
        ThrowIfDisposed();
        ValidateBufferArguments(buffer, offset, count);

        WriteCore(buffer.AsSpan(offset, count));
    }

    /// <inheritdoc/>
    public override void Write(ReadOnlySpan<byte> buffer)
    {
        ThrowIfDisposed();
        WriteCore(buffer);
    }

    /// <inheritdoc/>
    public override void WriteByte(byte value)
    {
        ThrowIfDisposed();

        var previousLength = _length;
        var newPosition = _position + 1;
        if (newPosition > _length)
        {
            EnsureCapacity(newPosition);
            if (_position > previousLength)
            {
                // Match MemoryStream: skipped bytes become zero-filled.
                _buffer.AsSpan(previousLength, _position - previousLength).Clear();
            }

            _length = newPosition;
        }

        _buffer[_position] = value;
        _position = newPosition;
    }

    // ─── MemoryStream-compatible Methods ──────────────────────────────────────────

    /// <summary>
    /// Returns the contents of the stream as a byte array.
    /// </summary>
    /// <returns>
    /// A new <see cref="byte"/> array containing all bytes written to the stream.
    /// This is a copy of the internal buffer.
    /// </returns>
    public byte[] ToArray()
    {
        ThrowIfDisposed();

        if (_length == 0) return [];

        var result = new byte[_length];
        Buffer.BlockCopy(_buffer, 0, result, 0, _length);
        return result;
    }

    /// <summary>
    /// Returns the written region of the internal buffer as a <see cref="ReadOnlyMemory{T}"/> without copying.
    /// </summary>
    /// <remarks>
    /// The returned <see cref="ReadOnlyMemory{T}"/> directly references the internal buffer.
    /// It is unsafe to use after further writes to the stream or after <see cref="Dispose"/>.
    /// </remarks>
    public ReadOnlyMemory<byte> GetBuffer()
    {
        ThrowIfDisposed();
        return _buffer.AsMemory(0, _length);
    }

    /// <summary>
    /// Returns the written region of the internal buffer as a <see cref="ReadOnlySpan{T}"/> without copying.
    /// </summary>
    /// <remarks>
    /// The returned <see cref="ReadOnlySpan{T}"/> directly references the internal buffer.
    /// It is unsafe to use after further writes to the stream or after <see cref="Dispose"/>.
    /// </remarks>
    public ReadOnlySpan<byte> GetBufferSpan()
    {
        ThrowIfDisposed();
        return _buffer.AsSpan(0, _length);
    }

    /// <summary>
    /// Writes the entire contents of this stream to another <see cref="Stream"/>.
    /// </summary>
    /// <param name="destination">The stream to write to.</param>
    public void WriteTo(Stream destination)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(destination);

        if (_length > 0)
            destination.Write(_buffer, 0, _length);
    }

    // ─── Dispose ──────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (_disposed) return;

        _disposed = true;

        var toReturn = _buffer;
        _buffer = [];
        _length = 0;
        _position = 0;

        // Array.Empty must not be returned to the pool
        if (toReturn.Length > 0)
            _pool.Return(toReturn);

        base.Dispose(disposing);
    }

    // ─── Private Helpers ──────────────────────────────────────────────────────────

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteCore(ReadOnlySpan<byte> source)
    {
        if (source.IsEmpty) return;

        var previousLength = _length;
        var newPosition = _position + source.Length;
        if (newPosition < 0) // Overflow check
            throw new IOException("The stream has exceeded its maximum size.");

        if (newPosition > _length)
        {
            EnsureCapacity(newPosition);
            if (_position > previousLength)
            {
                // Match MemoryStream: skipped bytes become zero-filled.
                _buffer.AsSpan(previousLength, _position - previousLength).Clear();
            }

            _length = newPosition;
        }

        source.CopyTo(_buffer.AsSpan(_position));
        _position = newPosition;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void EnsureCapacity(int required)
    {
        if (required <= _buffer.Length) return;

        // Double-growth strategy with upper bound
        var newCapacity = Math.Max(required, _buffer.Length == 0 ? DefaultInitialCapacity : _buffer.Length * 2);
        if ((uint)newCapacity > MaxArrayLength) newCapacity = MaxArrayLength;
        if (newCapacity < required)
            throw new IOException("The stream has exceeded its maximum size.");

        SetCapacity(newCapacity);
    }

    private void SetCapacity(int newCapacity)
    {
        if (newCapacity == _buffer.Length) return;

        if (newCapacity == 0)
        {
            if (_buffer.Length > 0)
                _pool.Return(_buffer);
            _buffer = [];
            return;
        }

        var newBuffer = _pool.Rent(newCapacity);

        if (_length > 0)
            Buffer.BlockCopy(_buffer, 0, newBuffer, 0, _length);

        if (_buffer.Length > 0)
            _pool.Return(_buffer);

        _buffer = newBuffer;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(PooledMemoryStream),
                "Cannot access a disposed stream.");
    }
}
