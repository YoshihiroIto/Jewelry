using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Jewelry.Memory;

public sealed class PooledList<T> : IEnumerable<T>, IDisposable
{
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public int Count => _count;

    private T[] _items;
    private int _count;
    private bool _disposed;
    private readonly int _maxPooledBufferElementSize;

    public PooledList(int capacity, int maxPooledBufferSize = -1)
    {
        if (capacity < 0)
            throw new ArgumentOutOfRangeException(nameof(capacity));

        if (maxPooledBufferSize > 0)
        {
            _maxPooledBufferElementSize = maxPooledBufferSize / Unsafe.SizeOf<T>();
            _items = capacity > _maxPooledBufferElementSize
                ? GC.AllocateUninitializedArray<T>(capacity)
                : ArrayPool<T>.Shared.Rent(capacity);
        }
        else
        {
            _maxPooledBufferElementSize = -1;
            _items = ArrayPool<T>.Shared.Rent(capacity);
        }

        _count = 0;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        var fromPool = _maxPooledBufferElementSize < 0 || _items.Length <= _maxPooledBufferElementSize;
        if (fromPool)
            ArrayPool<T>.Shared.Return(_items, clearArray: RuntimeHelpers.IsReferenceOrContainsReferences<T>());

        _items = null!;
        _count = 0;
        _disposed = true;
    }

    // Addメソッド
    public void Add(T item)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(PooledList<>));

        if (_count >= _items.Length)
            Resize();

        _items[_count] = item;
        _count++;
    }

    // Spanとして取得 (ゼロアロケーションでアクセス可能)
    public Span<T> AsSpan()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(PooledList<>));

        return new Span<T>(_items, 0, _count);
    }

    // IEnumerable<T>の実装
    public IEnumerator<T> GetEnumerator()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(PooledList<>));

        for (int i = 0; i < _count; i++)
        {
            yield return _items[i];
        }
    }

    public T this[int index]
    {
        get
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(PooledList<T>));

            if ((uint)index >= (uint)_count)
                throw new ArgumentOutOfRangeException(nameof(index));

            return _items[index];
        }
        set
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(PooledList<T>));

            if ((uint)index >= (uint)_count)
                throw new ArgumentOutOfRangeException(nameof(index));

            _items[index] = value;
        }
    }

    // 配列の拡張ロジック
    private void Resize()
    {
        var newSize = _items.Length is 0 ? 4 : _items.Length * 2;

        var newArray = _maxPooledBufferElementSize > 0 && newSize > _maxPooledBufferElementSize
            ? GC.AllocateUninitializedArray<T>(newSize)
            : ArrayPool<T>.Shared.Rent(newSize);

        Array.Copy(_items, newArray, _count);

        var returnOld = _maxPooledBufferElementSize < 0 || _items.Length <= _maxPooledBufferElementSize;
        if (returnOld)
            ArrayPool<T>.Shared.Return(_items, clearArray: RuntimeHelpers.IsReferenceOrContainsReferences<T>());

        _items = newArray;
    }
}
