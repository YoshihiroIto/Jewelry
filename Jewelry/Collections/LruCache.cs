using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Jewelry.Collections;

public class ThreadSafeLruCache<TKey, TValue>(int maxCapacity)
    : LruCacheBase<TKey, TValue, LruCacheBase.IsThreadSafe>(maxCapacity)
    where TKey : notnull;

public class LruCache<TKey, TValue>(int maxCapacity)
    : LruCacheBase<TKey, TValue, LruCacheBase.IsNotThreadSafe>(maxCapacity)
    where TKey : notnull;

public class LruCacheBase<TKey, TValue, TIsThreadSafe>
    where TKey : notnull
{
    protected LruCacheBase(int maxCapacity)
    {
        _maxCapacity = maxCapacity;

        if (typeof(TIsThreadSafe) == typeof(LruCacheBase.IsThreadSafe))
            _lockObj = new object();
    }

    protected virtual int GetValueSize(TValue value)
    {
        return 1;
    }

    protected virtual void OnDiscardedValue(TKey key, TValue value)
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        if (typeof(TIsThreadSafe) != typeof(LruCacheBase.IsThreadSafe))
            ClearInternal();

        else
        {
            lock (_lockObj!)
            {
                ClearInternal();
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(TKey key)
    {
        if (typeof(TIsThreadSafe) != typeof(LruCacheBase.IsThreadSafe))
            return ContainsInternal(key);

        else
        {
            lock (_lockObj!)
            {
                return ContainsInternal(key);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetValue(TKey key, out TValue value)
    {
        if (typeof(TIsThreadSafe) != typeof(LruCacheBase.IsThreadSafe))
            return TryGetValueInternal(key, out value);

        else
        {
            lock (_lockObj!)
            {
                return TryGetValueInternal(key, out value);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TValue Get(TKey key)
    {
        if (typeof(TIsThreadSafe) != typeof(LruCacheBase.IsThreadSafe))
            return GetInternal(key);

        else
        {
            lock (_lockObj!)
            {
                return GetInternal(key);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
    {
        if (typeof(TIsThreadSafe) != typeof(LruCacheBase.IsThreadSafe))
            return GetOrAddInternal(key, valueFactory);

        else
        {
            lock (_lockObj!)
            {
                return GetOrAddInternal(key, valueFactory);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(TKey key, TValue value)
    {
        if (typeof(TIsThreadSafe) != typeof(LruCacheBase.IsThreadSafe))
            AddInternal(key, value);

        else
        {
            lock (_lockObj!)
            {
                AddInternal(key, value);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Remove(TKey key)
    {
        if (typeof(TIsThreadSafe) != typeof(LruCacheBase.IsThreadSafe))
            RemoveInternal(key);

        else
        {
            lock (_lockObj!)
            {
                RemoveInternal(key);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ClearInternal()
    {
        foreach (var valueNode in _list)
            OnDiscardedValue(valueNode.Key, valueNode.Value);

        _list.Clear();
        _lookup.Clear();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool ContainsInternal(TKey key)
    {
        return _lookup.ContainsKey(key);
    }

    private bool TryGetValueInternal(TKey key, out TValue value)
    {
        if (_lookup.TryGetValue(key, out var listNode))
        {
            _list.Remove(listNode);
            _list.AddFirst(listNode);

            value = listNode.Value.Value;
            return true;
        }

        value = default!;
        return false;
    }

    private TValue GetInternal(TKey key)
    {
        if (_lookup.TryGetValue(key, out var listNode))
        {
            _list.Remove(listNode);
            _list.AddFirst(listNode);

            return listNode.Value.Value;
        }

        return default!;
    }

    private TValue GetOrAddInternal(TKey key, Func<TKey, TValue> valueFactory)
    {
        if (_lookup.TryGetValue(key, out var listNode))
        {
            _list.Remove(listNode);
            _list.AddFirst(listNode);

            return listNode.Value.Value;
        }

        var value = valueFactory(key);
        AddInternal(key, value);
        return value;
    }

    private void AddInternal(TKey key, TValue value)
    {
        if (_lookup.TryGetValue(key, out var listNode))
        {
            _currentSize -= GetValueSize(listNode.Value.Value);

            _list.Remove(listNode);
            _list.AddFirst(listNode);

            listNode.Value.Value = value;

            _currentSize += GetValueSize(listNode.Value.Value);
        }
        else
        {
            var keyValue = new KeyValue { Key = key, Value = value };

            listNode = _list.AddFirst(keyValue);

            _lookup.Add(key, listNode);

            _currentSize += GetValueSize(listNode.Value.Value);
        }

        while (_currentSize > _maxCapacity)
        {
            var valueNode = _list.Last ?? throw new NullReferenceException();

            _list.RemoveLast();
            _lookup.Remove(valueNode.Value.Key);

            _currentSize -= GetValueSize(valueNode.Value.Value);

            OnDiscardedValue(valueNode.Value.Key, valueNode.Value.Value);
        }
    }

    private void RemoveInternal(TKey key)
    {
        if (_lookup.TryGetValue(key, out var listNode) == false)
            return;

        OnDiscardedValue(listNode.Value.Key, listNode.Value.Value);

        _currentSize -= GetValueSize(listNode.Value.Value);

        _list.Remove(listNode);
        _lookup.Remove(key);
    }

    private class KeyValue
    {
        public TKey Key { get; set; } = default!;
        public TValue Value { get; set; } = default!;
    }

    private readonly LinkedList<KeyValue> _list = [];
    private readonly Dictionary<TKey, LinkedListNode<KeyValue>> _lookup = new();
    private readonly object? _lockObj;
    private readonly int _maxCapacity;
    private int _currentSize;
}

public class LruCacheBase
{
    public struct IsThreadSafe;

    public struct IsNotThreadSafe;
}