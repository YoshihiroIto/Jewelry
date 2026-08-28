using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Jewelry.Collections;

public sealed class ObservableHashSet<T> :
    ISet<T>,
    IReadOnlySet<T>,
    INotifyCollectionChanged,
    INotifyPropertyChanged
{
    public ObservableHashSet()
    {
        _items = [];
    }

    public ObservableHashSet(int capacity)
    {
        _items = new HashSet<T>(capacity);
    }

    public ObservableHashSet(IEqualityComparer<T>? comparer)
    {
        _items = new HashSet<T>(comparer);
    }

    public ObservableHashSet(int capacity, IEqualityComparer<T>? comparer)
    {
        _items = new HashSet<T>(capacity, comparer);
    }

    public ObservableHashSet(IEnumerable<T> collection)
    {
        _items = new HashSet<T>(collection);
    }

    public ObservableHashSet(IEnumerable<T> collection, IEqualityComparer<T>? comparer)
    {
        _items = new HashSet<T>(collection, comparer);
    }

    public event NotifyCollectionChangedEventHandler? CollectionChanged;
    public event PropertyChangedEventHandler? PropertyChanged;

    public int Count => _items.Count;
    public IEqualityComparer<T> Comparer => _items.Comparer;
    public bool IsReadOnly => false;

    public bool Add(T item)
    {
        CheckReentrancy();

        if (!_items.Add(item))
            return false;

        NotifyAdded(item);
        return true;
    }

    void ICollection<T>.Add(T item)
    {
        Add(item);
    }

    public bool Remove(T item)
    {
        CheckReentrancy();

        if (!_items.Remove(item))
            return false;

        NotifyRemoved(item);
        return true;
    }

    public void Clear()
    {
        CheckReentrancy();

        if (_items.Count is 0)
            return;

        _items.Clear();
        OnPropertyChanged(CountPropertyChangedEventArgs);
        OnCollectionChanged(ResetCollectionChangedEventArgs);
    }

    public bool Contains(T item)
    {
        return _items.Contains(item);
    }

    public bool TryGetValue(T equalValue, out T actualValue)
    {
        return _items.TryGetValue(equalValue, out actualValue!);
    }

    public void CopyTo(T[] array)
    {
        _items.CopyTo(array);
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        _items.CopyTo(array, arrayIndex);
    }

    public void CopyTo(T[] array, int arrayIndex, int count)
    {
        _items.CopyTo(array, arrayIndex, count);
    }

    public void UnionWith(IEnumerable<T> other)
    {
        var otherSet = Normalize(other);

        foreach (var item in otherSet)
            Add(item);
    }

    public void IntersectWith(IEnumerable<T> other)
    {
        var otherSet = Normalize(other);
        var currentItems = new List<T>(_items);

        foreach (var item in currentItems)
            if (!otherSet.Contains(item))
                Remove(item);
    }

    public void ExceptWith(IEnumerable<T> other)
    {
        var otherSet = Normalize(other);

        foreach (var item in otherSet)
            Remove(item);
    }

    public void SymmetricExceptWith(IEnumerable<T> other)
    {
        var otherSet = Normalize(other);

        foreach (var item in otherSet)
            if (!Remove(item))
                Add(item);
    }

    public int RemoveWhere(Predicate<T> match)
    {
        ArgumentNullException.ThrowIfNull(match);
        var removedCount = 0;
        var currentItems = new List<T>(_items);

        foreach (var item in currentItems)
            if (match(item) && Remove(item))
                ++removedCount;

        return removedCount;
    }

    public bool IsSubsetOf(IEnumerable<T> other)
    {
        return _items.IsSubsetOf(other);
    }

    public bool IsProperSubsetOf(IEnumerable<T> other)
    {
        return _items.IsProperSubsetOf(other);
    }

    public bool IsSupersetOf(IEnumerable<T> other)
    {
        return _items.IsSupersetOf(other);
    }

    public bool IsProperSupersetOf(IEnumerable<T> other)
    {
        return _items.IsProperSupersetOf(other);
    }

    public bool Overlaps(IEnumerable<T> other)
    {
        return _items.Overlaps(other);
    }

    public bool SetEquals(IEnumerable<T> other)
    {
        return _items.SetEquals(other);
    }

    public int EnsureCapacity(int capacity)
    {
        return _items.EnsureCapacity(capacity);
    }

    public void TrimExcess()
    {
        _items.TrimExcess();
    }

    public HashSet<T>.Enumerator GetEnumerator()
    {
        return _items.GetEnumerator();
    }

    IEnumerator<T> IEnumerable<T>.GetEnumerator()
    {
        return GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    private HashSet<T> Normalize(IEnumerable<T> other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return new HashSet<T>(other, Comparer);
    }

    private void NotifyAdded(T item)
    {
        OnPropertyChanged(CountPropertyChangedEventArgs);
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item));
    }

    private void NotifyRemoved(T item)
    {
        OnPropertyChanged(CountPropertyChangedEventArgs);
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item));
    }

    private void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        PropertyChanged?.Invoke(this, e);
    }

    private void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        var handlers = CollectionChanged;

        if (handlers is null)
            return;

        using (_monitor.Enter())
            handlers(this, e);
    }

    private void CheckReentrancy()
    {
        if (_monitor.IsBusy && CollectionChanged?.GetInvocationList().Length > 1)
            throw new InvalidOperationException("Cannot change ObservableHashSet during a CollectionChanged event.");
    }

    private readonly HashSet<T> _items;
    private readonly ObservableCollectionMonitor _monitor = new();

    private static readonly PropertyChangedEventArgs CountPropertyChangedEventArgs = new(nameof(Count));
    private static readonly NotifyCollectionChangedEventArgs ResetCollectionChangedEventArgs =
        new(NotifyCollectionChangedAction.Reset);
}
