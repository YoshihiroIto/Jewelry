using System;
using System.Collections.Generic;
using Jewelry.Disposable;
using Xunit;

namespace Jewelry.Test.Disposable;

public class CompactCompositeDisposableTests
{
    [Fact]
    public void 追加した破棄可能オブジェクトを追加順に一括破棄できること()
    {
        // Arrange
        var disposeOrder = new List<int>();
        var first = new TrackingDisposable(() => disposeOrder.Add(1));
        var second = new TrackingDisposable(() => disposeOrder.Add(2));
        var third = new TrackingDisposable(() => disposeOrder.Add(3));
        var disposables = new CompactCompositeDisposable();

        disposables.Add(first);
        disposables.Add(second);
        disposables.Add(third);

        // Act
        disposables.Dispose();

        // Assert
        Assert.True(first.IsDisposed);
        Assert.True(second.IsDisposed);
        Assert.True(third.IsDisposed);
        Assert.Equal([1, 2, 3], disposeOrder);
        Assert.True(disposables.IsDisposed);
        Assert.Equal(0, disposables.Count);
    }

    [Fact]
    public void 破棄後に追加した破棄可能オブジェクトは即時破棄されること()
    {
        // Arrange
        var disposables = new CompactCompositeDisposable();
        var disposable = new TrackingDisposable();

        disposables.Dispose();

        // Act
        disposables.Add(disposable);

        // Assert
        Assert.True(disposable.IsDisposed);
        Assert.Equal(0, disposables.Count);
    }

    [Fact]
    public void Clearはコレクションを破棄済みにせず現在の要素だけを破棄すること()
    {
        // Arrange
        var first = new TrackingDisposable();
        var second = new TrackingDisposable();
        var third = new TrackingDisposable();
        var disposables = new CompactCompositeDisposable(first, second);

        // Act
        disposables.Clear();
        disposables.Add(third);
        disposables.Dispose();

        // Assert
        Assert.True(first.IsDisposed);
        Assert.True(second.IsDisposed);
        Assert.True(third.IsDisposed);
        Assert.True(disposables.IsDisposed);
    }

    [Fact]
    public void 五件以上の破棄可能オブジェクトも追加順に一括破棄できること()
    {
        // Arrange
        var disposeOrder = new List<int>();
        var first = new TrackingDisposable(() => disposeOrder.Add(1));
        var second = new TrackingDisposable(() => disposeOrder.Add(2));
        var third = new TrackingDisposable(() => disposeOrder.Add(3));
        var fourth = new TrackingDisposable(() => disposeOrder.Add(4));
        var fifth = new TrackingDisposable(() => disposeOrder.Add(5));
        var sixth = new TrackingDisposable(() => disposeOrder.Add(6));
        var disposables = new CompactCompositeDisposable(first, second, third, fourth);

        disposables.Add(fifth);
        disposables.Add(sixth);

        // Act
        disposables.Dispose();

        // Assert
        Assert.Equal([1, 2, 3, 4, 5, 6], disposeOrder);
    }

    [Fact]
    public void Clear後に少数要素を再追加して破棄できること()
    {
        // Arrange
        var first = new TrackingDisposable();
        var second = new TrackingDisposable();
        var third = new TrackingDisposable();
        var disposables = new CompactCompositeDisposable(first, second);

        // Act
        disposables.Clear();
        disposables.Add(third);
        disposables.Dispose();

        // Assert
        Assert.True(first.IsDisposed);
        Assert.True(second.IsDisposed);
        Assert.True(third.IsDisposed);
        Assert.Equal(0, disposables.Count);
    }

    [Fact]
    public void AddToで追加した破棄可能オブジェクトをそのまま返すこと()
    {
        // Arrange
        var disposables = new CompactCompositeDisposable();
        var disposable = new TrackingDisposable();

        // Act
        var result = disposable.AddTo(disposables);
        disposables.Dispose();

        // Assert
        Assert.Same(disposable, result);
        Assert.True(disposable.IsDisposed);
    }

    [Fact]
    public void Nullを追加しようとすると例外が発生すること()
    {
        // Arrange
        var disposables = new CompactCompositeDisposable();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => disposables.Add(null!));
    }

    private sealed class TrackingDisposable(Action? onDispose = null) : IDisposable
    {
        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            if (IsDisposed)
                return;

            IsDisposed = true;
            onDispose?.Invoke();
        }
    }
}
