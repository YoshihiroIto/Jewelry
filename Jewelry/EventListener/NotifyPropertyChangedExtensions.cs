using System;
using System.ComponentModel;

namespace Jewelry.EventListener;

public static class NotifyPropertyChangedExtensions
{
    extension(INotifyPropertyChanged source)
    {
        public IDisposable CreatePropertyChangedEventListener(PropertyChangedEventHandler handler)
        {
            return new PropertyChangedEventListener(source, handler);
        }
    }
}

file sealed class PropertyChangedEventListener : IDisposable
{
    private Action<PropertyChangedEventHandler>? _remove;
    private PropertyChangedEventHandler? _handler;

    public PropertyChangedEventListener(INotifyPropertyChanged source, PropertyChangedEventHandler handler)
    {
        Initialize(
            h => source.PropertyChanged += h,
            h => source.PropertyChanged -= h,
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

    private void Initialize(Action<PropertyChangedEventHandler> add, Action<PropertyChangedEventHandler> remove,
        PropertyChangedEventHandler handler)
    {
        _handler = handler;
        _remove = remove;

        add(handler);
    }
}