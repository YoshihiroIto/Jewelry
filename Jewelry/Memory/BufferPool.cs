using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace Jewelry.Memory;

public class BufferPool<T>(int maxPooledBufferSize)
{
    private int MaxPooledBufferSize { get; } = maxPooledBufferSize / Unsafe.SizeOf<T>();

    public T[] Rent(int size)
    {
        return size > MaxPooledBufferSize
            ? GC.AllocateUninitializedArray<T>(size)
            : ArrayPool<T>.Shared.Rent(size);
    }

    public void Return(T[] data)
    {
        if (data.Length > MaxPooledBufferSize)
            return;

        ArrayPool<T>.Shared.Return(data, clearArray: RuntimeHelpers.IsReferenceOrContainsReferences<T>());
    }
}
