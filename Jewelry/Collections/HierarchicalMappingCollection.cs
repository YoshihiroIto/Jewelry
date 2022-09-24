using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;

namespace Jewelry.Collections
{
    public class HierarchicalMappingCollection<TSource, TTarget> : ObservableCollection<TTarget>, IDisposable
        where TSource : notnull
    {
        internal readonly Func<TSource, INotifyCollectionChanged> GetSourceChildren;
        internal readonly Func<TTarget, Collection<TTarget>> GetTargetChildren;

        private readonly INotifyCollectionChanged _source;
        private readonly Func<TSource, TTarget> _converter;
        private readonly bool _disposeElement;

        private readonly Dictionary<TSource, (TTarget, SourceCollectionChanged)> _itemDict = new();

        public HierarchicalMappingCollection(
            INotifyCollectionChanged source,
            Func<TSource, TTarget> converter,
            Func<TSource, INotifyCollectionChanged> getSourceChildren,
            Func<TTarget, Collection<TTarget>> getTargetChildren,
            bool disposeElement = true)
        {
            _source = source;
            _converter = converter;
            GetSourceChildren = getSourceChildren;
            GetTargetChildren = getTargetChildren;
            _disposeElement = disposeElement;

            _source.CollectionChanged += SourceOnCollectionChanged;

            foreach (var child in (IEnumerable<TSource>)_source)
                AddHierarchy(this, child);
        }

        public void Dispose()
        {
            foreach (var child in (IEnumerable<TSource>)_source)
                RemoveHierarchy(this, child);

            _source.CollectionChanged -= SourceOnCollectionChanged;
        }

        private void SourceOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    if (e.NewItems is not null)
                        foreach (TSource item in e.NewItems)
                            AddHierarchy(this, item);

                    break;

                case NotifyCollectionChangedAction.Remove:
                    if (e.OldItems is not null)
                        foreach (TSource item in e.OldItems)
                            RemoveHierarchy(this, item);

                    break;

                case NotifyCollectionChangedAction.Move:
                case NotifyCollectionChangedAction.Replace:
                case NotifyCollectionChangedAction.Reset:
                    throw new NotImplementedException();

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        internal void AddHierarchy(Collection<TTarget> targets, TSource sourceItem)
        {
            var targetItem = _converter(sourceItem);
            var scc = new SourceCollectionChanged(this, targetItem);

            _itemDict.Add(sourceItem, (targetItem, scc));
            targets.Add(targetItem);

            GetSourceChildren(sourceItem).CollectionChanged += scc.OnCollectionChanged;

            var targetChildren = GetTargetChildren(targetItem);

            var children = GetSourceChildren(sourceItem);
            foreach (var itemChild in (IEnumerable<TSource>)children)
                AddHierarchy(targetChildren, itemChild);
        }

        internal void RemoveHierarchy(Collection<TTarget> targets, TSource sourceItem)
        {
            Debug.Assert(_itemDict.ContainsKey(sourceItem));
            var (targetItem, sourceCollectionChanged) = _itemDict[sourceItem];

            var targetChildren = GetTargetChildren(targetItem);

            var children = GetSourceChildren(sourceItem);
            foreach (var itemChild in (IEnumerable<TSource>)children)
                RemoveHierarchy(targetChildren, itemChild);

            _itemDict.Remove(sourceItem);
            targets.Remove(targetItem);

            if (_disposeElement)
                if (targetItem is IDisposable disposable)
                    disposable.Dispose();

            GetSourceChildren(sourceItem).CollectionChanged -= sourceCollectionChanged.OnCollectionChanged;
        }

        private class SourceCollectionChanged
        {
            private readonly HierarchicalMappingCollection<TSource, TTarget> _parent;
            private readonly TTarget _target;

            public SourceCollectionChanged(HierarchicalMappingCollection<TSource, TTarget> parent, TTarget target)
            {
                _parent = parent;
                _target = target;
            }

            public void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
            {
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        if (e.NewItems is not null)
                            foreach (TSource item in e.NewItems)
                                _parent.AddHierarchy(_parent.GetTargetChildren(_target), item);

                        break;

                    case NotifyCollectionChangedAction.Remove:
                        if (e.OldItems is not null)
                            foreach (TSource item in e.OldItems)
                                _parent.RemoveHierarchy(_parent.GetTargetChildren(_target), item);

                        break;

                    case NotifyCollectionChangedAction.Move:
                    case NotifyCollectionChangedAction.Replace:
                    case NotifyCollectionChangedAction.Reset:
                        throw new NotImplementedException();

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
    }

    public static class HierarchicalMappingCollectionExtensions
    {
        public static HierarchicalMappingCollection<TSource, TTarget>
            ToHierarchicalMappingCollection<TSource, TTarget>(
                this INotifyCollectionChanged self,
                Func<TSource, TTarget> converter,
                Func<TSource, INotifyCollectionChanged> getSourceChildren,
                Func<TTarget, Collection<TTarget>> getTargetChildren,
                bool disposeElement = true)
            where TSource : notnull
            => new(self, converter, getSourceChildren, getTargetChildren, disposeElement);
    }
}