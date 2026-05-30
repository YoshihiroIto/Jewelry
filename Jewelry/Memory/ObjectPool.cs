using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Jewelry.Memory;

public sealed class ObjectPool<T>(int maxPoolCount)
    where T : new()
{
    private readonly Stack<T> _pool = new();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Get()
    {
        if (_pool.TryPop(out var result))
            return result;

        return new T();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Return(T obj)
    {
        if (_pool.Count >= maxPoolCount)
            return;

        _pool.Push(obj);
    }
}
