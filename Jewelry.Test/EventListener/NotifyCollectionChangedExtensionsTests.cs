using System.Collections.Specialized;
using Jewelry.EventListener;
using Xunit;

namespace Jewelry.Test.EventListener;

public class NotifyCollectionChangedExtensionsTests
{
    private class TestObservableCollection : INotifyCollectionChanged
    {
        public event NotifyCollectionChangedEventHandler? CollectionChanged;

        public void RaiseCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            CollectionChanged?.Invoke(this, e);
        }
    }

    [Fact]
    public void CreateCollectionChangedEventListener_RegistersHandler()
    {
        // Arrange
        var collection = new TestObservableCollection();
        var handlerCalled = false;
        NotifyCollectionChangedEventHandler handler = (_, _) => handlerCalled = true;

        // Act
        using (collection.CreateCollectionChangedEventListener(handler))
        {
            collection.RaiseCollectionChanged(
                new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));

            // Assert
            Assert.True(handlerCalled);
        }
    }

    [Fact]
    public void DisposeCollectionChangedEventListener_UnregistersHandler()
    {
        // Arrange
        var collection = new TestObservableCollection();
        var handlerCalled = false;
        NotifyCollectionChangedEventHandler handler = (_, _) => handlerCalled = true;

        var listener = collection.CreateCollectionChangedEventListener(handler);

        // Act
        listener.Dispose();
        collection.RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));

        // Assert
        Assert.False(handlerCalled);
    }
}
