using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Jewelry.Collections;
using Xunit;

namespace Jewelry.Test.Collections;

public sealed class ObservableDictionaryTests
{
    [Fact]
    public void ConstructorsAndInterfacesExposeDictionaryState()
    {
        var source = new Dictionary<string, int> { ["One"] = 1 };
        var dictionary = new ObservableDictionary<string, int>(source, StringComparer.OrdinalIgnoreCase);

        Assert.IsAssignableFrom<IDictionary<string, int>>(dictionary);
        Assert.IsAssignableFrom<IReadOnlyDictionary<string, int>>(dictionary);
        Assert.IsAssignableFrom<INotifyCollectionChanged>(dictionary);
        Assert.IsAssignableFrom<INotifyPropertyChanged>(dictionary);
        Assert.Same(StringComparer.OrdinalIgnoreCase, dictionary.Comparer);
        Assert.Equal(1, dictionary["one"]);
        Assert.Contains("One", dictionary.Keys);
        Assert.Contains(1, dictionary.Values);

        var withCapacity = new ObservableDictionary<string, int>(16, StringComparer.Ordinal);
        Assert.True(withCapacity.EnsureCapacity(16) >= 16);
        withCapacity.TrimExcess();
        withCapacity.TrimExcess(0);
    }

    [Fact]
    public void AddThroughEveryApiRaisesAddNotifications()
    {
        var dictionary = new ObservableDictionary<string, int>();
        var changes = Observe(dictionary);

        dictionary.Add("one", 1);
        Assert.True(dictionary.TryAdd("two", 2));
        ((ICollection<KeyValuePair<string, int>>)dictionary).Add(new("three", 3));
        ((IDictionary<string, int>)dictionary).Add("four", 4);

        Assert.Equal(4, dictionary.Count);
        Assert.All(changes.CollectionChanges, x => Assert.Equal(NotifyCollectionChangedAction.Add, x.Action));
        Assert.All(changes.CollectionChanges, x => Assert.Equal(-1, x.NewStartingIndex));
        Assert.Equal(
            [
                new KeyValuePair<string, int>("one", 1),
                new KeyValuePair<string, int>("two", 2),
                new KeyValuePair<string, int>("three", 3),
                new KeyValuePair<string, int>("four", 4)
            ],
            changes.NewItems);
        Assert.Equal(4, changes.PropertyNames.Count(x => x == nameof(dictionary.Count)));
        Assert.Equal(4, changes.PropertyNames.Count(x => x == nameof(dictionary.Keys)));
        Assert.Equal(4, changes.PropertyNames.Count(x => x == nameof(dictionary.Values)));
        Assert.Equal(4, changes.PropertyNames.Count(x => x == "Item[]"));
    }

    [Fact]
    public void DuplicateAddAndTryAddNoOpDoNotNotify()
    {
        var dictionary = new ObservableDictionary<string, int> { ["one"] = 1 };
        var changes = Observe(dictionary);

        Assert.False(dictionary.TryAdd("one", 10));
        Assert.Throws<ArgumentException>(() => dictionary.Add("one", 10));

        Assert.Empty(changes.CollectionChanges);
        Assert.Empty(changes.PropertyNames);
        Assert.Equal(1, dictionary["one"]);
    }

    [Fact]
    public void IndexerAddsOrReplacesWithOldAndNewPairs()
    {
        var dictionary = new ObservableDictionary<string, int>();
        var changes = Observe(dictionary);

        dictionary["one"] = 1;
        dictionary["one"] = 10;

        Assert.Equal(10, dictionary["one"]);
        Assert.Equal(NotifyCollectionChangedAction.Add, changes.CollectionChanges[0].Action);
        var replace = changes.CollectionChanges[1];
        Assert.Equal(NotifyCollectionChangedAction.Replace, replace.Action);
        Assert.Equal(new KeyValuePair<string, int>("one", 1), Assert.Single(replace.OldItems!));
        Assert.Equal(new KeyValuePair<string, int>("one", 10), Assert.Single(replace.NewItems!));
        Assert.Equal(
            [nameof(dictionary.Count), nameof(dictionary.Keys), nameof(dictionary.Values), "Item[]",
                nameof(dictionary.Values), "Item[]"],
            changes.PropertyNames);
    }

    [Fact]
    public void RemoveApisNotifyOnlyWhenAnEntryIsRemoved()
    {
        var dictionary = new ObservableDictionary<string, int>
        {
            ["one"] = 1,
            ["two"] = 2,
            ["three"] = 3
        };
        var changes = Observe(dictionary);

        Assert.False(((ICollection<KeyValuePair<string, int>>)dictionary).Remove(new("one", 10)));
        Assert.True(((ICollection<KeyValuePair<string, int>>)dictionary).Remove(new("one", 1)));
        Assert.True(dictionary.Remove("two", out var removedValue));
        Assert.Equal(2, removedValue);
        Assert.True(dictionary.Remove("three"));
        Assert.False(dictionary.Remove("missing"));

        Assert.Empty(dictionary);
        Assert.Equal(3, changes.CollectionChanges.Count);
        Assert.All(changes.CollectionChanges, x => Assert.Equal(-1, x.OldStartingIndex));
        Assert.Equal(
            [
                new KeyValuePair<string, int>("one", 1),
                new KeyValuePair<string, int>("two", 2),
                new KeyValuePair<string, int>("three", 3)
            ],
            changes.OldItems);
    }

    [Fact]
    public void ClearRaisesOneResetAndEmptyClearIsSilent()
    {
        var dictionary = new ObservableDictionary<string, int>
        {
            ["one"] = 1,
            ["two"] = 2
        };
        var changes = Observe(dictionary);

        dictionary.Clear();
        dictionary.Clear();

        var change = Assert.Single(changes.CollectionChanges);
        Assert.Equal(NotifyCollectionChangedAction.Reset, change.Action);
        Assert.Equal([nameof(dictionary.Count), nameof(dictionary.Keys), nameof(dictionary.Values), "Item[]"],
            changes.PropertyNames);
    }

    [Fact]
    public void ReadAndCopyApisMatchDictionaryBehavior()
    {
        var dictionary = new ObservableDictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["one"] = 1,
            ["two"] = 2
        };

        Assert.True(dictionary.ContainsKey("ONE"));
        Assert.True(dictionary.ContainsValue(2));
        Assert.True(dictionary.TryGetValue("TWO", out var value));
        Assert.Equal(2, value);
        Assert.True(((ICollection<KeyValuePair<string, int>>)dictionary).Contains(new("ONE", 1)));

        var copy = new KeyValuePair<string, int>[3];
        ((ICollection<KeyValuePair<string, int>>)dictionary).CopyTo(copy, 1);
        Assert.Equal(2, copy.Skip(1).Count(x => x.Key is not null));
        Assert.Equal(2, dictionary.Count());
    }

    [Fact]
    public void PropertyNotificationsPrecedeCollectionNotification()
    {
        var dictionary = new ObservableDictionary<string, int>();
        var order = new List<string?>();
        dictionary.PropertyChanged += (_, e) => order.Add(e.PropertyName);
        dictionary.CollectionChanged += (_, _) => order.Add("CollectionChanged");

        dictionary.Add("one", 1);

        Assert.Equal(
            [nameof(dictionary.Count), nameof(dictionary.Keys), nameof(dictionary.Values), "Item[]", "CollectionChanged"],
            order);
    }

    [Fact]
    public void MultipleCollectionSubscribersPreventReentrantMutation()
    {
        var dictionary = new ObservableDictionary<string, int>();
        dictionary.CollectionChanged += (_, _) => dictionary.Add("nested", 2);
        dictionary.CollectionChanged += (_, _) => { };

        Assert.Throws<InvalidOperationException>(() => dictionary.Add("outer", 1));
        Assert.True(dictionary.ContainsKey("outer"));
        Assert.False(dictionary.ContainsKey("nested"));
    }

    private static ChangeRecorder Observe(ObservableDictionary<string, int> dictionary)
    {
        var recorder = new ChangeRecorder();
        dictionary.PropertyChanged += (_, e) => recorder.PropertyNames.Add(e.PropertyName);
        dictionary.CollectionChanged += (_, e) =>
        {
            recorder.CollectionChanges.Add(e);

            if (e.NewItems is not null)
                foreach (KeyValuePair<string, int> item in e.NewItems)
                    recorder.NewItems.Add(item);

            if (e.OldItems is not null)
                foreach (KeyValuePair<string, int> item in e.OldItems)
                    recorder.OldItems.Add(item);
        };
        return recorder;
    }

    private sealed class ChangeRecorder
    {
        public List<NotifyCollectionChangedEventArgs> CollectionChanges { get; } = [];
        public List<string?> PropertyNames { get; } = [];
        public List<KeyValuePair<string, int>> NewItems { get; } = [];
        public List<KeyValuePair<string, int>> OldItems { get; } = [];
    }
}
