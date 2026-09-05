using System;
using System.ComponentModel;
using Jewelry.EventListener;
using Xunit;

namespace Jewelry.Test.EventListener;

public class WeakEventListenerTests
{
    [Fact]
    public void 作成メソッドはイベントハンドラを正しく追加する()
    {
        // Arrange
        var source = new EventSource();
        var handlerCalled = false;
        EventHandler<PropertyChangedEventArgs> handler = (_, _) => handlerCalled = true;

        // Act
        using (WeakEventListener.Create<PropertyChangedEventHandler, PropertyChangedEventArgs>(
                   h => new PropertyChangedEventHandler(h),
                   h => source.PropertyChanged += h,
                   h => source.PropertyChanged -= h,
                   handler))
        {
            source.RaisePropertyChanged();
        }

        // Assert
        Assert.True(handlerCalled);
    }

    [Fact]
    public void 破棄メソッドはイベントハンドラを正しく削除する()
    {
        // Arrange
        var source = new EventSource();
        var handlerCalled = false;
        EventHandler<PropertyChangedEventArgs> handler = (_, _) => handlerCalled = true;

        var listener = WeakEventListener.Create<PropertyChangedEventHandler, PropertyChangedEventArgs>(
            h => new PropertyChangedEventHandler(h),
            h => source.PropertyChanged += h,
            h => source.PropertyChanged -= h,
            handler);

        // Act
        listener.Dispose();
        source.RaisePropertyChanged(); // Should not call handler

        // Assert
        Assert.False(handlerCalled);
    }

    [Fact]
    public void リスナーがgcされたときにイベントが解除される()
    {
        // Arrange
        var source = new EventSource();
        var handlerCalled = false;
        EventHandler<PropertyChangedEventArgs> handler = (_, _) => handlerCalled = true;

        CreateWeakListener(source, handler);

        // Act
        GC.Collect();
        GC.WaitForPendingFinalizers();
        source.RaisePropertyChanged();

        // Assert
        Assert.False(handlerCalled);
    }

    private void CreateWeakListener(EventSource source, EventHandler<PropertyChangedEventArgs> handler)
    {
        WeakEventListener.Create<PropertyChangedEventHandler, PropertyChangedEventArgs>(
            h => new PropertyChangedEventHandler(h),
            h => source.PropertyChanged += h,
            h => source.PropertyChanged -= h,
            handler);
    }

    private class EventSource : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public void RaisePropertyChanged()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RaisePropertyChanged)));
        }
    }
}
