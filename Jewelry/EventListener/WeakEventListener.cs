using System;

namespace Jewelry.EventListener;

public static class WeakEventListener
{
    public static IDisposable Create<THandler, TEventArgs>(
        Func<EventHandler<TEventArgs>, THandler> conversion,
        Action<THandler> add,
        Action<THandler> remove,
        EventHandler<TEventArgs> handler)
        where TEventArgs : EventArgs
    {
        return new InternalWeakEventListener<THandler, TEventArgs>(conversion, add, remove, handler);
    }
}

// ref: https://github.com/runceel/Livet/blob/master/LivetCask.EventListeners/EventListeners/WeakEvents/LivetWeakEventListener.cs
file sealed class InternalWeakEventListener<THandler, TEventArgs> : IDisposable
    where TEventArgs : EventArgs
{
    private EventHandler<TEventArgs>? _handler;
    private Action<THandler>? _remove;
    private THandler? _resultHandler;

    public InternalWeakEventListener(
        Func<EventHandler<TEventArgs>, THandler> conversion,
        Action<THandler> add,
        Action<THandler> remove,
        EventHandler<TEventArgs> handler)
    {
        _handler = handler;
        _remove = remove;

        _resultHandler = GetStaticHandler(new WeakReference<InternalWeakEventListener<THandler, TEventArgs>>(this),
            conversion);

        add(_resultHandler);
    }

    public void Dispose()
    {
        _ = _remove ?? throw new ArgumentNullException(nameof(_remove));
        _ = _resultHandler ?? throw new ArgumentNullException(nameof(_resultHandler));

        _remove.Invoke(_resultHandler);
        _handler = null;
        _resultHandler = default;
        _remove = null;
    }

    private static void ReceiveEvent(
        WeakReference<InternalWeakEventListener<THandler, TEventArgs>> listenerWeakReference,
        object? sender,
        TEventArgs args)
    {
        if (listenerWeakReference.TryGetTarget(out var listenerResult))
            listenerResult._handler?.Invoke(sender, args);
    }

    private static THandler GetStaticHandler(
        WeakReference<InternalWeakEventListener<THandler, TEventArgs>> listenerWeakReference,
        Func<EventHandler<TEventArgs>, THandler> conversion)
    {
        return conversion((sender, e) => ReceiveEvent(listenerWeakReference, sender, e));
    }
}