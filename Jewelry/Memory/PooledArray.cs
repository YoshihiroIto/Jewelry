using System;
using System.Buffers;
using System.Collections.Generic;

namespace Jewelry.Memory;

public static class PooledArrayHelper
{
    public static PooledArray<T> ToPooledArray<T>(this IEnumerable<T> source)
    {
        switch (source)
        {
            case T[] array:
                return new PooledArray<T>(array, array.Length, false);

            case ICollection<T> { Count: 0 }:
                return new PooledArray<T>(Array.Empty<T>(), 0, false);

            case IReadOnlyCollection<T> { Count: 0 }:
                return new PooledArray<T>(Array.Empty<T>(), 0, false);

            case ICollection<T> collection:
                {
                    var rentArray = new PooledArray<T>(collection.Count);
                    collection.CopyTo(rentArray.Array, 0);
                    return rentArray;
                }
        }

        var index = 0;
        var bufferArray = ArrayPool<T>.Shared.Rent(32);

        foreach (var item in source)
        {
            if (bufferArray.Length <= index)
            {
                var newSize = bufferArray.Length * 2;
                var newArray = ArrayPool<T>.Shared.Rent(index < newSize ? newSize : index * 2);

                Array.Copy(bufferArray, 0, newArray, 0, bufferArray.Length);
                ArrayPool<T>.Shared.Return(bufferArray);

                bufferArray = newArray;
            }

            bufferArray[index] = item;
            ++index;
        }

        return new PooledArray<T>(bufferArray, index, true);
    }
}

public readonly ref struct PooledArray<T>
{
    public readonly int Length;
    internal readonly T[] Array;
    private readonly bool _shouldReturnPool;

    public Span<T> AsSpan() => Array.AsSpan(0, Length);

    internal PooledArray(int length)
    {
        Array = ArrayPool<T>.Shared.Rent(length);
        Length = length;

        _shouldReturnPool = true;
    }

    internal PooledArray(T[] array, int length, bool shouldReturnPool)
    {
        Array = array;
        Length = length;

        _shouldReturnPool = shouldReturnPool;
    }

    public void Dispose()
    {
        if (_shouldReturnPool == false)
            return;

        ArrayPool<T>.Shared.Return(Array, typeof(T).IsValueType == false);
    }
}