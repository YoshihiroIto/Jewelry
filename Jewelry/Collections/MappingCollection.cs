using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace Jewelry.Collections;

public static class ReadOnlyMappingCollectionExtensions
{
    public static ReadOnlyMappingCollection<TSource, TTarget> ToReadOnlyMappingCollection<TSource, TTarget>(
        this ObservableCollection<TSource> self,
        Func<TSource, TTarget> converter,
        bool disposeElement = true)
        where TSource : notnull
    {
        var collection = new MappingCollection<TSource, TTarget>(self, converter, disposeElement);
        return new ReadOnlyMappingCollection<TSource, TTarget>(collection);
    }

    public static ReadOnlyMappingCollection<TSource, TTarget> ToReadOnlyMappingCollection<TSource, TTarget>(
        this ReadOnlyObservableCollection<TSource> self,
        Func<TSource, TTarget> converter,
        bool disposeElement = true)
        where TSource : notnull
    {
        var collection = new MappingCollection<TSource, TTarget>(self, converter, disposeElement);
        return new ReadOnlyMappingCollection<TSource, TTarget>(collection);
    }
}

public sealed class ReadOnlyMappingCollection<TSource, TTarget> : ReadOnlyCollection<TTarget>, IDisposable
    where TSource : notnull
{
    private readonly IDisposable _source;

    internal ReadOnlyMappingCollection(MappingCollection<TSource, TTarget> source)
        : base(source)
    {
        _source = source;
    }

    public void Dispose()
    {
        _source.Dispose();
    }
}

internal class MappingCollection<TSource, TTarget> : ObservableCollection<TTarget>, IDisposable
    where TSource : notnull
{
    private readonly INotifyCollectionChanged _source;
    private readonly Func<TSource, TTarget> _converter;
    private readonly bool _disposeElement;

    public MappingCollection(
        INotifyCollectionChanged source,
        Func<TSource, TTarget> converter,
        bool disposeElement = true)
    {
        _source = source;
        _converter = converter;
        _disposeElement = disposeElement;

        _source.CollectionChanged += SourceOnCollectionChanged;

        foreach (var item in (IEnumerable<TSource>)_source)
            Add(_converter(item));
    }

    public void Dispose()
    {
        _source.CollectionChanged -= SourceOnCollectionChanged;

        foreach (var item in this)
        {
            if (_disposeElement)
                (item as IDisposable)?.Dispose();
        }
    }

    private void SourceOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
            {
                _ = e.NewItems ?? throw new InvalidOperationException();
                var index = e.NewStartingIndex;

                foreach (TSource item in e.NewItems)
                {
                    Insert(index, _converter(item));
                    ++index;
                }

                break;
            }

            case NotifyCollectionChangedAction.Remove:
            {
                _ = e.OldItems ?? throw new InvalidOperationException();
                var index = e.OldStartingIndex;

                for (var i = 0; i != e.OldItems.Count; ++i)
                {
                    if (_disposeElement)
                        (this[index] as IDisposable)?.Dispose();

                    RemoveAt(index);
                }

                break;
            }

            case NotifyCollectionChangedAction.Move:
            {
                _ = e.NewItems ?? throw new InvalidOperationException();
                _ = e.OldItems ?? throw new InvalidOperationException();

                if (e.NewItems.Count is not 1)
                    throw new NotSupportedException();
                if (e.OldItems.Count is not 1)
                    throw new NotSupportedException();

                Move(e.OldStartingIndex, e.NewStartingIndex);

                break;
            }

            case NotifyCollectionChangedAction.Replace:
                throw new NotImplementedException();

            case NotifyCollectionChangedAction.Reset:
            {
                if (_disposeElement)
                    foreach (var item in this)
                        (item as IDisposable)?.Dispose();

                Clear();
                break;
            }

            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}
