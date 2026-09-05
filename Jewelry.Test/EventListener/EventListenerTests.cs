using System;
using Xunit;
using Listener = Jewelry.EventListener.EventListener;

namespace Jewelry.Test.EventListener;

public class EventListenerTests
{
    [Fact]
    public void 作成メソッドはイベントハンドラを追加する()
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
    public void 破棄メソッドはイベントハンドラを削除する()
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
    public void 破棄を複数回呼び出してもイベントハンドラは一度だけ削除される()
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
