using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace Jewelry.Collections;

public sealed class ObservableCollectionEx<T> : ObservableCollection<T>
{
    public void BeginChange()
    {
        Interlocked.Increment(ref _changingDepth);
    }

    public void EndChange()
    {
        var c = Interlocked.Decrement(ref _changingDepth);

        Debug.Assert(c >= 0);

        if (c == 0)
            OnCollectionChanged(ResetEventArgs);
    }

    public void AddRange(IEnumerable<T> items)
    {
        foreach (var item in items)
            Items.Add(item);

        if (IsInChanging == false)
            OnCollectionChanged(ResetEventArgs);
    }

    public void AddRange(ReadOnlySpan<T> items)
    {
        foreach (var item in items)
            Items.Add(item);

        if (IsInChanging == false)
            OnCollectionChanged(ResetEventArgs);
    }

    public new void Clear()
    {
        if (IsInChanging)
        {
            if (Items.Count > 0)
                Items.Clear();
        }
        else
            base.Clear();
    }

    public new void Add(T item)
    {
        if (IsInChanging)
            Items.Add(item);
        else
            base.Add(item);
    }

    public new bool Remove(T item)
    {
        return IsInChanging ? Items.Remove(item) : base.Remove(item);
    }

    public new void Insert(int index, T item)
    {
        if (IsInChanging)
            Items.Insert(index, item);
        else
            base.Insert(index, item);
    }

    public Span<T> AsSpan()
    {
        if (Items is not List<T> list)
            throw new NotSupportedException();

        return CollectionsMarshal.AsSpan(list);
    }

    public Span<T> AsSpan(int start)
    {
        return AsSpan()[start..];
    }

    public Span<T> AsSpan(int start, int length)
    {
        return AsSpan().Slice(start, length);
    }
    
    private bool IsInChanging => _changingDepth > 0;
    private int _changingDepth;

    // ReSharper disable once StaticMemberInGenericType
    private static readonly NotifyCollectionChangedEventArgs ResetEventArgs =
        new(NotifyCollectionChangedAction.Reset);
}