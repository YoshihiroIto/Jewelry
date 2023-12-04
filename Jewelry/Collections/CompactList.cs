using System;
using System.Collections;
using System.Collections.Generic;

namespace Jewelry.Collections;

// Implemented using this code as a reference
// https://github.com/JetBrains/rd/blob/master/rd-net/Lifetimes/Collections/CompactList.cs

public struct CompactList<T> : IEnumerable<T>
{
    public CompactListEnumerator<T> GetEnumerator() => new(_singleValue, _multipleValues);
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public int Count => _multipleValues == IsSingle ? 1 : _multipleValues?.Count ?? 0;
    
    internal static readonly List<T?> IsSingle = new();
    private T? _singleValue;
    private List<T?>? _multipleValues;

    public void Add(T item)
    {
        switch (Count)
        {
            case 0:
                _singleValue = item;
                _multipleValues = IsSingle;
                break;

            case 1:
                _multipleValues = new List<T?> { _singleValue, item };
                _singleValue = default;
                break;

            default:
                _ = _multipleValues ?? throw new InvalidOperationException();
                _multipleValues.Add(item);
                break;
        }
    }

    public void Clear()
    {
        _singleValue = default;
        _multipleValues = default;
    }

    public T? this[int index]
    {
        get
        {
            if (_multipleValues == IsSingle)
            {
                if (index == 0)
                    return _singleValue;

                throw new IndexOutOfRangeException();
            }

            if (_multipleValues is null)
                throw new IndexOutOfRangeException();

            if(index >= Count)
                throw new IndexOutOfRangeException();
                
            return _multipleValues[index];
        }
    }
}

public struct CompactListEnumerator<T> : IEnumerator<T?>
{
    public T? Current { get; private set; }
    object? IEnumerator.Current => Current;
    
    private readonly T? _singleValue;
    private readonly List<T?>? _multipleValues;
    private int _index;

    internal CompactListEnumerator(T? singleValue, List<T?>? multipleValues)
    {
        _singleValue = singleValue;
        _multipleValues = multipleValues;
        _index = -1;
    }

    public bool MoveNext()
    {
        if (_multipleValues is null)
            return false;

        ++_index;

        if (_multipleValues == CompactList<T>.IsSingle)
        {
            if (_index == 0)
            {
                Current = _singleValue;
                return true;
            }
        }
        else
        {
            if (_index < _multipleValues.Count)
            {
                Current = _multipleValues[_index];
                return true;
            }
        }

        Current = default;
        return false;
    }

    public void Reset()
    {
        _index = -1;
        Current = default;
    }

    public void Dispose()
    {
    }
}

public static class CompactListExtensions
{
    public static CompactList<T> ToCompactList<T>(this IEnumerable<T> source)
    {
        var result = new CompactList<T>();

        foreach (var item in source)
            result.Add(item);

        return result;
    }
}