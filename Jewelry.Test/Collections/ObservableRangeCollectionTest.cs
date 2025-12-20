using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Jewelry.Collections;
using Xunit;

namespace Jewelry.Test.Collections;

public class ObservableRangeCollectionTest
{
    private class PropertyChangeTracker
    {
        private readonly List<string> _firedProperties = new();

        public PropertyChangeTracker(INotifyPropertyChanged obj)
        {
            obj.PropertyChanged += (_, args) => _firedProperties.Add(args.PropertyName!);
        }
        
        public IReadOnlyList<string> FiredProperties => _firedProperties;
    }

    [Fact]
    public void ConstructorDefault()
    {
        var sut = new ObservableRangeCollection<int>();
        Assert.Empty(sut);
    }

    [Fact]
    public void ConstructorWithBehavior()
    {
        var sut = new ObservableRangeCollection<int>(RangeRemoveBehavior.Reset);
        Assert.Empty(sut);
    }

    [Fact]
    public void ConstructorWithCollection()
    {
        var sut = new ObservableRangeCollection<int>(new[] { 1, 2, 3 });
        Assert.Equal(new[] { 1, 2, 3 }, sut);
    }

    [Fact]
    public void ConstructorWithCollectionAndBehavior()
    {
        var sut = new ObservableRangeCollection<int>(new[] { 1, 2, 3 }, RangeRemoveBehavior.Iterative);
        Assert.Equal(new[] { 1, 2, 3 }, sut);
    }

    [Fact]
    public void AddRange_FiresPropertyChanged()
    {
        var sut = new ObservableRangeCollection<int>();
        var tracker = new PropertyChangeTracker(sut);
        
        sut.AddRange(new []{1, 2, 3});
        
        Assert.Contains("Count", tracker.FiredProperties);
        Assert.Contains("Item[]", tracker.FiredProperties);
    }
    
    [Fact]
    public void InsertRange_FiresPropertyChanged()
    {
        var sut = new ObservableRangeCollection<int>(new[]{1, 2, 3});
        var tracker = new PropertyChangeTracker(sut);
        
        sut.InsertRange(1, new []{4, 5});
        
        Assert.Contains("Count", tracker.FiredProperties);
        Assert.Contains("Item[]", tracker.FiredProperties);
    }
    
    [Fact]
    public void RemoveRange_FiresPropertyChanged()
    {
        var sut = new ObservableRangeCollection<int>(new[]{1, 2, 3}, RangeRemoveBehavior.Reset);
        var tracker = new PropertyChangeTracker(sut);
        
        sut.RemoveRange(1, 2);
        
        Assert.Contains("Count", tracker.FiredProperties);
        Assert.Contains("Item[]", tracker.FiredProperties);
    }
    
    [Fact]
    public void ReplaceAll_FiresPropertyChanged()
    {
        var sut = new ObservableRangeCollection<int>(new[]{1, 2, 3});
        var tracker = new PropertyChangeTracker(sut);
        
        sut.ReplaceAll(new []{4, 5});
        
        Assert.Contains("Count", tracker.FiredProperties);
        Assert.Contains("Item[]", tracker.FiredProperties);
    }
    
    [Fact]
    public void AddRange()
    {
        var sut = new ObservableRangeCollection<int>(new[] { 1, 2, 3 });
        var newItems = new[] { 4, 5, 6 };

        var collectionChanged = new List<NotifyCollectionChangedEventArgs>();
        sut.CollectionChanged += (_, e) => collectionChanged.Add(e);

        sut.AddRange(newItems);

        Assert.Equal(new[] { 1, 2, 3, 4, 5, 6 }, sut);
        Assert.Single(collectionChanged);
        Assert.Equal(NotifyCollectionChangedAction.Add, collectionChanged[0].Action);
        Assert.Equal(newItems, collectionChanged[0].NewItems!.Cast<int>());
        Assert.Equal(3, collectionChanged[0].NewStartingIndex);
    }

    [Fact]
    public void InsertRange()
    {
        var sut = new ObservableRangeCollection<int>(new[] { 1, 2, 3 });
        var newItems = new[] { 4, 5, 6 };

        var collectionChanged = new List<NotifyCollectionChangedEventArgs>();
        sut.CollectionChanged += (_, e) => collectionChanged.Add(e);

        sut.InsertRange(1, newItems);

        Assert.Equal(new[] { 1, 4, 5, 6, 2, 3 }, sut);
        Assert.Single(collectionChanged);
        Assert.Equal(NotifyCollectionChangedAction.Add, collectionChanged[0].Action);
        Assert.Equal(newItems, collectionChanged[0].NewItems!.Cast<int>());
        Assert.Equal(1, collectionChanged[0].NewStartingIndex);
    }
    
    [Fact]
    public void InsertRangeNull()
    {
        var sut = new ObservableRangeCollection<int>();
        Assert.Throws<ArgumentNullException>(() => sut.InsertRange(0, null!));
    }
    
    [Fact]
    public void InsertRangeOutOfRange()
    {
        var sut = new ObservableRangeCollection<int>();
        Assert.Throws<ArgumentOutOfRangeException>(() => sut.InsertRange(1, new []{1}));
        Assert.Throws<ArgumentOutOfRangeException>(() => sut.InsertRange(-1, new []{1}));
    }

    [Fact]
    public void RemoveRange_Iterative()
    {
        var sut = new ObservableRangeCollection<int>(new[] { 1, 2, 3, 4, 5, 6 }, RangeRemoveBehavior.Iterative);
        
        var collectionChanged = new List<NotifyCollectionChangedEventArgs>();
        sut.CollectionChanged += (_, e) => collectionChanged.Add(e);

        sut.RemoveRange(1, 3);

        Assert.Equal(new[] { 1, 5, 6 }, sut);
        Assert.Equal(3, collectionChanged.Count);

        Assert.Equal(NotifyCollectionChangedAction.Remove, collectionChanged[0].Action);
        Assert.Equal(new[] { 2 }, collectionChanged[0].OldItems!.Cast<int>());
        Assert.Equal(1, collectionChanged[0].OldStartingIndex);

        Assert.Equal(NotifyCollectionChangedAction.Remove, collectionChanged[1].Action);
        Assert.Equal(new[] { 3 }, collectionChanged[1].OldItems!.Cast<int>());
        Assert.Equal(1, collectionChanged[1].OldStartingIndex);

        Assert.Equal(NotifyCollectionChangedAction.Remove, collectionChanged[2].Action);
        Assert.Equal(new[] { 4 }, collectionChanged[2].OldItems!.Cast<int>());
        Assert.Equal(1, collectionChanged[2].OldStartingIndex);
    }
    
    [Fact]
    public void RemoveRange_Reset()
    {
        var sut = new ObservableRangeCollection<int>(new[] { 1, 2, 3, 4, 5, 6 }, RangeRemoveBehavior.Reset);
        
        var collectionChanged = new List<NotifyCollectionChangedEventArgs>();
        sut.CollectionChanged += (_, e) => collectionChanged.Add(e);

        sut.RemoveRange(1, 3);

        Assert.Equal(new[] { 1, 5, 6 }, sut);
        Assert.Single(collectionChanged);
        Assert.Equal(NotifyCollectionChangedAction.Reset, collectionChanged[0].Action);
    }
    
    [Fact]
    public void RemoveRange_CountZero_DoesNothing()
    {
        var original = new[] { 1, 2, 3 };
        var sut = new ObservableRangeCollection<int>(original);

        var collectionChangedFired = false;
        var propertyChangedFired = false;
        sut.CollectionChanged += (_, _) => collectionChangedFired = true;
        ((INotifyPropertyChanged)sut).PropertyChanged += (_, _) => propertyChangedFired = true;
        
        sut.RemoveRange(1, 0);
        
        Assert.Equal(original, sut);
        Assert.False(collectionChangedFired);
        Assert.False(propertyChangedFired);
    }

    [Fact]
    public void RemoveRange_Smart_UnderThreshold()
    {
        var sut = new ObservableRangeCollection<int>(Enumerable.Range(0, 60), RangeRemoveBehavior.Smart);
        
        var collectionChanged = new List<NotifyCollectionChangedEventArgs>();
        sut.CollectionChanged += (_, e) => collectionChanged.Add(e);

        sut.RemoveRange(10, 50); // Threshold is 50, so this is Iterative
        
        Assert.Equal(50, collectionChanged.Count);
        Assert.All(collectionChanged, e => Assert.Equal(NotifyCollectionChangedAction.Remove, e.Action));
    }

    [Fact]
    public void RemoveRange_Smart_OverThreshold()
    {
        var sut = new ObservableRangeCollection<int>(Enumerable.Range(0, 51), RangeRemoveBehavior.Smart);
        
        var collectionChanged = new List<NotifyCollectionChangedEventArgs>();
        sut.CollectionChanged += (_, e) => collectionChanged.Add(e);

        sut.RemoveRange(0, 51); // Threshold is 50, so this is Reset
        
        Assert.Single(collectionChanged);
        Assert.Equal(NotifyCollectionChangedAction.Reset, collectionChanged[0].Action);
    }


    [Fact]
    public void RemoveRange_OutOfRange()
    {
        var sut = new ObservableRangeCollection<int>(new[] { 1, 2, 3 });
        Assert.Throws<ArgumentOutOfRangeException>(() => sut.RemoveRange(4, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => sut.RemoveRange(-1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => sut.RemoveRange(0, 4));
        Assert.Throws<ArgumentOutOfRangeException>(() => sut.RemoveRange(0, -1));
    }
    
    [Fact]
    public void ReplaceAll()
    {
        var sut = new ObservableRangeCollection<int>(new[] { 1, 2, 3 });
        var newItems = new[] { 4, 5, 6 };

        var collectionChanged = new List<NotifyCollectionChangedEventArgs>();
        sut.CollectionChanged += (_, e) => collectionChanged.Add(e);

        sut.ReplaceAll(newItems);

        Assert.Equal(new[] { 4, 5, 6 }, sut);
        Assert.Single(collectionChanged);
        Assert.Equal(NotifyCollectionChangedAction.Reset, collectionChanged[0].Action);
    }
    
    [Fact]
    public void ReplaceAll_WithEmpty_ClearsCollection()
    {
        var sut = new ObservableRangeCollection<int>(new[] { 1, 2, 3 });
        
        var collectionChanged = new List<NotifyCollectionChangedEventArgs>();
        sut.CollectionChanged += (_, e) => collectionChanged.Add(e);
        
        sut.ReplaceAll(Array.Empty<int>());
        
        Assert.Empty(sut);
        Assert.Single(collectionChanged);
        Assert.Equal(NotifyCollectionChangedAction.Reset, collectionChanged[0].Action);
    }

    [Fact]
    public void ReplaceAll_Null()
    {
        var sut = new ObservableRangeCollection<int>();
        Assert.Throws<ArgumentNullException>(() => sut.ReplaceAll(null!));
    }
    
    [Fact]
    public void AsSpan_ReturnsCorrectSpan()
    {
        var source = new[] { 1, 2, 3, 4, 5 };
        var sut = new ObservableRangeCollection<int>(source);

        var span = sut.AsSpan();
        Assert.Equal(source.Length, span.Length);
        Assert.True(source.SequenceEqual(span.ToArray()));
    }
    
    [Fact]
    public void AsSpan_WithStart_ReturnsCorrectSpan()
    {
        var source = new[] { 1, 2, 3, 4, 5 };
        var sut = new ObservableRangeCollection<int>(source);

        var span = sut.AsSpan(2);
        Assert.Equal(3, span.Length);
        Assert.True(source.AsSpan(2).SequenceEqual(span));
    }

    [Fact]
    public void AsSpan_WithStartAndLength_ReturnsCorrectSpan()
    {
        var source = new[] { 1, 2, 3, 4, 5 };
        var sut = new ObservableRangeCollection<int>(source);

        var span = sut.AsSpan(1, 3);
        Assert.Equal(3, span.Length);
        Assert.True(source.AsSpan(1, 3).SequenceEqual(span));
    }

    [Fact]
    public void AsSpan_Modification_AffectsOriginalCollection()
    {
        var source = new[] { 1, 2, 3, 4, 5 };
        var sut = new ObservableRangeCollection<int>(source);

        var span = sut.AsSpan();
        span[2] = 99;

        Assert.Equal(99, sut[2]);
    }
    
    [Fact]
    public void EnsureCapacity_IncreasesCapacity()
    {
        var sut = new ObservableRangeCollection<int>();
        var initialCapacity = sut.EnsureCapacity(0); // Get current capacity
        
        var newCapacity = sut.EnsureCapacity(initialCapacity + 10);
        
        Assert.True(newCapacity >= initialCapacity + 10);
    }
    
    [Fact]
    public void TrimExcess_ReducesCapacity()
    {
        var sut = new ObservableRangeCollection<int>();
        sut.EnsureCapacity(100);
        
        sut.Add(1);
        sut.Add(2);
        
        var capacityBeforeTrim = sut.EnsureCapacity(0);
        sut.TrimExcess();
        var capacityAfterTrim = sut.EnsureCapacity(0);

        Assert.True(capacityAfterTrim < capacityBeforeTrim);
        Assert.True(capacityAfterTrim >= sut.Count);
    }
    
    // Helper class to create an ObservableRangeCollection that doesn't use List<T> internally
    private class CustomCollection<T> : Collection<T>
    {
        public CustomCollection(IList<T> list) : base(list) { }
    }

    private ObservableRangeCollection<T> CreateCollectionWithNonListInternal<T>()
    {
        var customList = new CustomCollection<T>(new List<T>());
        
        // Use reflection to set the internal 'Items' field to our custom collection
        var collection = new ObservableRangeCollection<T>();
        var itemsField = typeof(Collection<T>).GetField("items", BindingFlags.NonPublic | BindingFlags.Instance);
        itemsField!.SetValue(collection, customList);
        
        return collection;
    }

    [Fact]
    public void AsSpan_Throws_When_Not_List()
    {
        var sut = CreateCollectionWithNonListInternal<int>();
        Assert.Throws<NotSupportedException>(() => sut.AsSpan());
    }

    [Fact]
    public void EnsureCapacity_Throws_When_Not_List()
    {
        var sut = CreateCollectionWithNonListInternal<int>();
        Assert.Throws<NotSupportedException>(() => sut.EnsureCapacity(10));
    }
    
    [Fact]
    public void TrimExcess_Throws_When_Not_List()
    {
        var sut = CreateCollectionWithNonListInternal<int>();
        Assert.Throws<NotSupportedException>(() => sut.TrimExcess());
    }
}
