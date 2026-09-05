using System.ComponentModel;
using Jewelry.EventListener;
using Xunit;

namespace Jewelry.Test.EventListener;

public class NotifyPropertyChangedExtensionsTests
{
    private class TestObservableObject : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    [Fact]
    public void CreatePropertyChangedEventListener_RegistersHandler()
    {
        // Arrange
        var obj = new TestObservableObject();
        var handlerCalled = false;
        PropertyChangedEventHandler handler = (_, _) => handlerCalled = true;

        // Act
        using (obj.CreatePropertyChangedEventListener(handler))
        {
            obj.RaisePropertyChanged("TestProperty");

            // Assert
            Assert.True(handlerCalled);
        }
    }

    [Fact]
    public void DisposePropertyChangedEventListener_UnregistersHandler()
    {
        // Arrange
        var obj = new TestObservableObject();
        var handlerCalled = false;
        PropertyChangedEventHandler handler = (_, _) => handlerCalled = true;

        var listener = obj.CreatePropertyChangedEventListener(handler);

        // Act
        listener.Dispose();
        obj.RaisePropertyChanged("TestProperty");

        // Assert
        Assert.False(handlerCalled);
    }
}
