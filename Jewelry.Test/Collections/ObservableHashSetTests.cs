using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Jewelry.Collections;
using Xunit;

namespace Jewelry.Test.Collections;

public sealed class ObservableHashSetTests
{
    [Fact]
    public void ConstructorsAndInterfacesExposeSetState()
    {
        var set = new ObservableHashSet<string>(["One", "ONE", "Two"], StringComparer.OrdinalIgnoreCase);

        Assert.IsAssignableFrom<ISet<string>>(set);
        Assert.IsAssignableFrom<IReadOnlySet<string>>(set);
        Assert.IsAssignableFrom<INotifyCollectionChanged>(set);
        Assert.IsAssignableFrom<INotifyPropertyChanged>(set);
        Assert.Same(StringComparer.OrdinalIgnoreCase, set.Comparer);
        Assert.Equal(2, set.Count);
        Assert.Equal("One", Assert.Single(set, x => string.Equals(x, "one", StringComparison.OrdinalIgnoreCase)));

        var withCapacity = new ObservableHashSet<string>(16, StringComparer.Ordinal);
        Assert.True(withCapacity.EnsureCapacity(16) >= 16);
        withCapacity.TrimExcess();
    }

    [Fact]
    public void AddAndRemoveThroughInterfacesNotifyOnlyForChanges()
    {
        var set = new ObservableHashSet<int>();
        var changes = Observe(set);

        Assert.True(set.Add(1));
        Assert.False(set.Add(1));
        ((ICollection<int>)set).Add(2);
        Assert.False(set.Remove(3));
        Assert.True(((ISet<int>)set).Remove(1));

        Assert.Equal([2], set);
        Assert.Equal(
            [NotifyCollectionChangedAction.Add, NotifyCollectionChangedAction.Add,
                NotifyCollectionChangedAction.Remove],
            changes.CollectionChanges.Select(x => x.Action));
        Assert.Equal([-1, -1, -1], changes.CollectionChanges.Select(x =>
            x.Action is NotifyCollectionChangedAction.Add ? x.NewStartingIndex : x.OldStartingIndex));
        Assert.Equal([1, 2], changes.NewItems);
        Assert.Equal([1], changes.OldItems);
        Assert.Equal(3, changes.PropertyNames.Count(x => x == nameof(set.Count)));
    }

    [Fact]
    public void ClearRaisesOneResetAndEmptyClearIsSilent()
    {
        var set = new ObservableHashSet<int>([1, 2]);
        var changes = Observe(set);

        set.Clear();
        set.Clear();

        var change = Assert.Single(changes.CollectionChanges);
        Assert.Equal(NotifyCollectionChangedAction.Reset, change.Action);
        Assert.Equal([nameof(set.Count)], changes.PropertyNames);
    }

    [Fact]
    public void UnionWithNormalizesDuplicatesAndNotifiesEachAddedItem()
    {
        var set = new ObservableHashSet<int>([1, 2]);
        var changes = Observe(set);

        set.UnionWith([2, 3, 3, 4]);

        Assert.True(set.SetEquals([1, 2, 3, 4]));
        Assert.Equal([3, 4], changes.NewItems);
        Assert.All(changes.CollectionChanges, x => Assert.Equal(NotifyCollectionChangedAction.Add, x.Action));
    }

    [Fact]
    public void IntersectWithNotifiesEachRemovedItem()
    {
        var set = new ObservableHashSet<int>([1, 2, 3, 4]);
        var changes = Observe(set);

        set.IntersectWith([2, 4, 4]);

        Assert.True(set.SetEquals([2, 4]));
        Assert.True(changes.OldItems.ToHashSet().SetEquals([1, 3]));
        Assert.All(changes.CollectionChanges, x => Assert.Equal(NotifyCollectionChangedAction.Remove, x.Action));
    }

    [Fact]
    public void ExceptWithSupportsSelfAndNotifiesEachRemovedItem()
    {
        var set = new ObservableHashSet<int>([1, 2, 3]);
        var changes = Observe(set);

        set.ExceptWith(set);

        Assert.Empty(set);
        Assert.True(changes.OldItems.ToHashSet().SetEquals([1, 2, 3]));
        Assert.Equal(3, changes.CollectionChanges.Count);
    }

    [Fact]
    public void SymmetricExceptWithTreatsDuplicateInputAsOneItem()
    {
        var set = new ObservableHashSet<int>([1, 2]);
        var changes = Observe(set);

        set.SymmetricExceptWith([2, 2, 3, 3]);

        Assert.True(set.SetEquals([1, 3]));
        Assert.Equal([2], changes.OldItems);
        Assert.Equal([3], changes.NewItems);
    }

    [Fact]
    public void RemoveWhereNotifiesEachRemovedItem()
    {
        var set = new ObservableHashSet<int>([1, 2, 3, 4]);
        var changes = Observe(set);

        var removed = set.RemoveWhere(x => x % 2 is 0);

        Assert.Equal(2, removed);
        Assert.True(set.SetEquals([1, 3]));
        Assert.True(changes.OldItems.ToHashSet().SetEquals([2, 4]));
    }

    [Fact]
    public void ReadCopyAndRelationApisMatchHashSetBehavior()
    {
        var storedValue = new Value(1, "stored");
        var set = new ObservableHashSet<Value>([storedValue], new ValueComparer());

        Assert.True(set.TryGetValue(new Value(1, "probe"), out var actual));
        Assert.Same(storedValue, actual);
        Assert.True(set.IsSubsetOf([storedValue, new Value(2, "two")]));
        Assert.True(set.IsProperSubsetOf([storedValue, new Value(2, "two")]));
        Assert.True(set.IsSupersetOf([new Value(1, "other")]));
        Assert.False(set.IsProperSupersetOf([new Value(1, "other")]));
        Assert.True(set.Overlaps([new Value(1, "other")]));

        var copy = new Value[2];
        set.CopyTo(copy, 1);
        Assert.Same(storedValue, copy[1]);
        Assert.Single(set);
    }

    [Fact]
    public void PropertyNotificationPrecedesCollectionNotification()
    {
        var set = new ObservableHashSet<int>();
        var order = new List<string?>();
        set.PropertyChanged += (_, e) => order.Add(e.PropertyName);
        set.CollectionChanged += (_, _) => order.Add("CollectionChanged");

        set.Add(1);

        Assert.Equal([nameof(set.Count), "CollectionChanged"], order);
    }

    [Fact]
    public void MultipleCollectionSubscribersPreventReentrantMutation()
    {
        var set = new ObservableHashSet<int>();
        set.CollectionChanged += (_, _) => set.Add(2);
        set.CollectionChanged += (_, _) => { };

        Assert.Throws<InvalidOperationException>(() => set.Add(1));
        Assert.Contains(1, (IEnumerable<int>)set);
        Assert.DoesNotContain(2, (IEnumerable<int>)set);
    }

    private static ChangeRecorder Observe(ObservableHashSet<int> set)
    {
        var recorder = new ChangeRecorder();
        set.PropertyChanged += (_, e) => recorder.PropertyNames.Add(e.PropertyName);
        set.CollectionChanged += (_, e) =>
        {
            recorder.CollectionChanges.Add(e);

            if (e.NewItems is not null)
                foreach (int item in e.NewItems)
                    recorder.NewItems.Add(item);

            if (e.OldItems is not null)
                foreach (int item in e.OldItems)
                    recorder.OldItems.Add(item);
        };
        return recorder;
    }

    private sealed class ChangeRecorder
    {
        public List<NotifyCollectionChangedEventArgs> CollectionChanges { get; } = [];
        public List<string?> PropertyNames { get; } = [];
        public List<int> NewItems { get; } = [];
        public List<int> OldItems { get; } = [];
    }

    private sealed record Value(int Id, string Name);

    private sealed class ValueComparer : IEqualityComparer<Value>
    {
        public bool Equals(Value? x, Value? y)
        {
            return x?.Id == y?.Id;
        }

        public int GetHashCode(Value obj)
        {
            return obj.Id;
        }
    }
}
