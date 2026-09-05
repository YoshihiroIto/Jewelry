using System;
using Xunit;
using Listener = Jewelry.EventListener.EventListener;

namespace Jewelry.Test.EventListener;

public class EventListenerTests
{
    [Fact]
    public void Create_AddsEventHandler()
    {
        // Arrange
        var eventAdded = false;
        Action<EventHandler> addAction = _ => eventAdded = true;
        Action<EventHandler> removeAction = _ => { };
        EventHandler handler = (_, _) => { };

        // Act
        using (Listener.Create(addAction, removeAction, handler))
        {
            // Assert
            Assert.True(eventAdded);
        }
    }

    [Fact]
    public void Dispose_RemovesEventHandler()
    {
        // Arrange
        var eventRemoved = false;
        Action<EventHandler> addAction = _ => { };
        Action<EventHandler> removeAction = _ => eventRemoved = true;
        EventHandler handler = (_, _) => { };

        // Act
        var listener = Listener.Create(addAction, removeAction, handler);
        listener.Dispose();

        // Assert
        Assert.True(eventRemoved);
    }

    [Fact]
    public void Dispose_RemovesEventHandlerOnlyOnce_WhenCalledMultipleTimes()
    {
        // Arrange
        var removeCallCount = 0;
        Action<EventHandler> addAction = _ => { };
        Action<EventHandler> removeAction = _ => removeCallCount++;
        EventHandler handler = (_, _) => { };

        // Act
        var listener = Listener.Create(addAction, removeAction, handler);
        listener.Dispose();
        listener.Dispose(); // Call Dispose multiple times

        // Assert
        Assert.Equal(1, removeCallCount);
    }
}
