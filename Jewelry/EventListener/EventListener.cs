using System;

namespace Jewelry.EventListener;

public static class EventListener
{
    public static IDisposable Create<THandler>(
        Action<THandler> add,
        Action<THandler> remove,
        THandler handler)
    {
        return new InternalEventListener<THandler>(add, remove, handler);
    }
}

file sealed class InternalEventListener<THandler> : IDisposable
{
    private readonly Action<THandler> _remove;
    private readonly THandler _handler;

    private bool _disposed;

    public InternalEventListener(
        Action<THandler> add,
        Action<THandler> remove,
        THandler handler)
    {
        _remove = remove;
        _handler = handler;

        add(handler);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _remove.Invoke(_handler);
        _disposed = true;
    }
}