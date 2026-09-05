using System;
using System.Collections.Specialized;

namespace Jewelry.EventListener;

public static class NotifyCollectionChangedExtensions
{
    extension(INotifyCollectionChanged source)
    {
        public IDisposable CreateCollectionChangedEventListener(NotifyCollectionChangedEventHandler handler)
        {
            return new CollectionChangedEventListener(source, handler);
        }
    }
}

file sealed class CollectionChangedEventListener : IDisposable
{
    private Action<NotifyCollectionChangedEventHandler>? _remove;
    private NotifyCollectionChangedEventHandler? _handler;

    public CollectionChangedEventListener(INotifyCollectionChanged source, NotifyCollectionChangedEventHandler handler)
    {
        Initialize(
            h => source.CollectionChanged += h,
            h => source.CollectionChanged -= h,
            handler);
    }

    public void Dispose()
    {
        _ = _remove ?? throw new ArgumentNullException(nameof(_remove));
        _ = _handler ?? throw new ArgumentNullException(nameof(_handler));

        _remove.Invoke(_handler);
        _remove = null;
        _handler = null;
    }

    private void Initialize(Action<NotifyCollectionChangedEventHandler> add,
        Action<NotifyCollectionChangedEventHandler> remove,
        NotifyCollectionChangedEventHandler handler)
    {
        _handler = handler;
        _remove = remove;

        add(handler);
    }
}