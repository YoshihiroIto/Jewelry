using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Jewelry.Collections;

public sealed class ObservableDictionary<TKey, TValue> :
    IDictionary<TKey, TValue>,
    IReadOnlyDictionary<TKey, TValue>,
    INotifyCollectionChanged,
    INotifyPropertyChanged
    where TKey : notnull
{
    public ObservableDictionary()
    {
        _items = new Dictionary<TKey, TValue>();
    }

    public ObservableDictionary(int capacity)
    {
        _items = new Dictionary<TKey, TValue>(capacity);
    }

    public ObservableDictionary(IEqualityComparer<TKey>? comparer)
    {
        _items = new Dictionary<TKey, TValue>(comparer);
    }

    public ObservableDictionary(int capacity, IEqualityComparer<TKey>? comparer)
    {
        _items = new Dictionary<TKey, TValue>(capacity, comparer);
    }

    public ObservableDictionary(IDictionary<TKey, TValue> dictionary)
    {
        _items = new Dictionary<TKey, TValue>(dictionary);
    }

    public ObservableDictionary(
        IDictionary<TKey, TValue> dictionary,
        IEqualityComparer<TKey>? comparer)
    {
        _items = new Dictionary<TKey, TValue>(dictionary, comparer);
    }

    public event NotifyCollectionChangedEventHandler? CollectionChanged;
    public event PropertyChangedEventHandler? PropertyChanged;

    public TValue this[TKey key]
    {
        get => _items[key];
        set
        {
            CheckReentrancy();

            if (_items.TryGetValue(key, out var oldValue))
            {
                var oldItem = new KeyValuePair<TKey, TValue>(key, oldValue);
                var newItem = new KeyValuePair<TKey, TValue>(key, value);
                _items[key] = value;

                OnPropertyChanged(ValuesPropertyChangedEventArgs);
                OnPropertyChanged(IndexerPropertyChangedEventArgs);
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(
                    NotifyCollectionChangedAction.Replace,
                    newItem,
                    oldItem));
            }
            else
            {
                _items[key] = value;
                NotifyAdded(new KeyValuePair<TKey, TValue>(key, value));
            }
        }
    }

    public int Count => _items.Count;
    public Dictionary<TKey, TValue>.KeyCollection Keys => _items.Keys;
    public Dictionary<TKey, TValue>.ValueCollection Values => _items.Values;
    public IEqualityComparer<TKey> Comparer => _items.Comparer;
    public bool IsReadOnly => false;

    ICollection<TKey> IDictionary<TKey, TValue>.Keys => Keys;
    ICollection<TValue> IDictionary<TKey, TValue>.Values => Values;
    IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => Keys;
    IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => Values;

    public void Add(TKey key, TValue value)
    {
        CheckReentrancy();
        _items.Add(key, value);
        NotifyAdded(new KeyValuePair<TKey, TValue>(key, value));
    }

    public bool TryAdd(TKey key, TValue value)
    {
        CheckReentrancy();

        if (!_items.TryAdd(key, value))
            return false;

        NotifyAdded(new KeyValuePair<TKey, TValue>(key, value));
        return true;
    }

    public bool ContainsKey(TKey key)
    {
        return _items.ContainsKey(key);
    }

    public bool ContainsValue(TValue value)
    {
        return _items.ContainsValue(value);
    }

    public bool TryGetValue(TKey key, out TValue value)
    {
        return _items.TryGetValue(key, out value!);
    }

    public bool Remove(TKey key)
    {
        CheckReentrancy();

        if (!_items.Remove(key, out var value))
            return false;

        NotifyRemoved(new KeyValuePair<TKey, TValue>(key, value));
        return true;
    }

    public bool Remove(TKey key, out TValue value)
    {
        CheckReentrancy();

        if (!_items.Remove(key, out value!))
            return false;

        NotifyRemoved(new KeyValuePair<TKey, TValue>(key, value));
        return true;
    }

    public void Clear()
    {
        CheckReentrancy();

        if (_items.Count is 0)
            return;

        _items.Clear();
        OnPropertyChanged(CountPropertyChangedEventArgs);
        OnPropertyChanged(KeysPropertyChangedEventArgs);
        OnPropertyChanged(ValuesPropertyChangedEventArgs);
        OnPropertyChanged(IndexerPropertyChangedEventArgs);
        OnCollectionChanged(ResetCollectionChangedEventArgs);
    }

    public int EnsureCapacity(int capacity)
    {
        return _items.EnsureCapacity(capacity);
    }

    public void TrimExcess()
    {
        _items.TrimExcess();
    }

    public void TrimExcess(int capacity)
    {
        _items.TrimExcess(capacity);
    }

    public Dictionary<TKey, TValue>.Enumerator GetEnumerator()
    {
        return _items.GetEnumerator();
    }

    IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
    {
        return GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
    {
        Add(item.Key, item.Value);
    }

    bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
    {
        return ((ICollection<KeyValuePair<TKey, TValue>>)_items).Contains(item);
    }

    void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(
        KeyValuePair<TKey, TValue>[] array,
        int arrayIndex)
    {
        ((ICollection<KeyValuePair<TKey, TValue>>)_items).CopyTo(array, arrayIndex);
    }

    bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
    {
        CheckReentrancy();
        var collection = (ICollection<KeyValuePair<TKey, TValue>>)_items;

        if (!collection.Remove(item))
            return false;

        NotifyRemoved(item);
        return true;
    }

    private void NotifyAdded(KeyValuePair<TKey, TValue> item)
    {
        OnPropertyChanged(CountPropertyChangedEventArgs);
        OnPropertyChanged(KeysPropertyChangedEventArgs);
        OnPropertyChanged(ValuesPropertyChangedEventArgs);
        OnPropertyChanged(IndexerPropertyChangedEventArgs);
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item));
    }

    private void NotifyRemoved(KeyValuePair<TKey, TValue> item)
    {
        OnPropertyChanged(CountPropertyChangedEventArgs);
        OnPropertyChanged(KeysPropertyChangedEventArgs);
        OnPropertyChanged(ValuesPropertyChangedEventArgs);
        OnPropertyChanged(IndexerPropertyChangedEventArgs);
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
            throw new InvalidOperationException("Cannot change ObservableDictionary during a CollectionChanged event.");
    }

    private readonly Dictionary<TKey, TValue> _items;
    private readonly ObservableCollectionMonitor _monitor = new();

    private static readonly PropertyChangedEventArgs CountPropertyChangedEventArgs = new(nameof(Count));
    private static readonly PropertyChangedEventArgs KeysPropertyChangedEventArgs = new(nameof(Keys));
    private static readonly PropertyChangedEventArgs ValuesPropertyChangedEventArgs = new(nameof(Values));
    private static readonly PropertyChangedEventArgs IndexerPropertyChangedEventArgs = new("Item[]");
    private static readonly NotifyCollectionChangedEventArgs ResetCollectionChangedEventArgs =
        new(NotifyCollectionChangedAction.Reset);
}
