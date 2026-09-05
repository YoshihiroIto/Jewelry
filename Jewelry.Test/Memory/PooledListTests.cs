using System;
using System.Linq;
using Jewelry.Memory;
using Xunit;

namespace Jewelry.Test.Memory;

public sealed class PooledListTests
{
    [Fact]
    public void Add_IncreasesCountAndPreservesOrder()
    {
        // Arrange
        using var list = new PooledList<int>(2);

        // Act
        list.Add(1);
        list.Add(2);
        list.Add(3); // 容量拡張が起きても順序が維持されるべき

        // Assert
        Assert.Equal(3, list.Count);
        Assert.Equal([1, 2, 3], list.ToArray());
    }

    [Fact]
    public void Resize_PreservesAllItems_WithSmallInitialCapacity()
    {
        // Arrange
        using var list = new PooledList<int>(1);
        var expected = Enumerable.Range(0, 100).ToArray();

        // Act
        foreach (var i in expected)
            list.Add(i);

        // Assert
        Assert.Equal(expected.Length, list.Count);
        Assert.Equal(expected, list.ToArray());
    }

    [Fact]
    public void AsSpan_ReturnsItemsUpToCount()
    {
        // Arrange
        using var list = new PooledList<string>(2);
        list.Add("a");
        list.Add("b");

        // Act
        var span = list.AsSpan();
        var i0 = span[0];
        var i1 = span[1];

        // Assert
        Assert.Equal(2, span.Length);
        Assert.Equal("a", i0);
        Assert.Equal("b", i1);
    }

    [Fact]
    public void Enumeration_ReturnsItemsInOrder()
    {
        // Arrange
        using var list = new PooledList<int>(3);
        list.Add(10);
        list.Add(20);
        list.Add(30);

        // Act
        var enumerated = list.ToArray();

        // Assert
        Assert.Equal([10, 20, 30], enumerated);
    }

    [Fact]
    public void Add_ThrowsObjectDisposedException_AfterDisposal()
    {
        // Arrange
        var list = new PooledList<int>(1);
        list.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => list.Add(1));
    }

    [Fact]
    public void AsSpan_ThrowsObjectDisposedException_AfterDisposal()
    {
        // Arrange
        var list = new PooledList<int>(1);
        list.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => _ = list.AsSpan());
    }

    [Fact]
    public void Enumeration_ThrowsObjectDisposedException_AfterDisposal()
    {
        // Arrange
        var list = new PooledList<int>(1);
        list.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => _ = list.ToArray());
    }

    [Fact]
    public void IndexerGet_ReturnsAddedItems()
    {
        // Arrange
        using var list = new PooledList<int>(2);
        list.Add(10);
        list.Add(20);

        // Act
        var first = list[0];
        var second = list[1];

        // Assert
        Assert.Equal(10, first);
        Assert.Equal(20, second);
    }

    [Fact]
    public void IndexerSet_UpdatesExistingItems()
    {
        // Arrange
        using var list = new PooledList<int>(1);
        list.Add(1);
        list.Add(2);

        // Act
        list[0] = 100;
        list[1] = 200;

        // Assert
        Assert.Equal(100, list[0]);
        Assert.Equal(200, list[1]);
    }

    [Fact]
    public void IndexerGet_ThrowsArgumentOutOfRangeException_ForOutOfRangeIndex()
    {
        // Arrange
        using var list = new PooledList<int>(1);
        list.Add(42);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => _ = list[-1]);
        Assert.Throws<ArgumentOutOfRangeException>(() => _ = list[1]);
    }

    [Fact]
    public void IndexerSet_ThrowsArgumentOutOfRangeException_ForOutOfRangeIndex()
    {
        // Arrange
        using var list = new PooledList<int>(1);
        list.Add(0);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => list[-1] = 1);
        Assert.Throws<ArgumentOutOfRangeException>(() => list[1] = 1);
    }

    [Fact]
    public void IndexerGet_ThrowsObjectDisposedException_AfterDisposal()
    {
        // Arrange
        var list = new PooledList<int>(1);
        list.Add(7);
        list.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => _ = list[0]);
    }

    [Fact]
    public void IndexerSet_ThrowsObjectDisposedException_AfterDisposal()
    {
        // Arrange
        var list = new PooledList<int>(1);
        list.Add(7);
        list.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => list[0] = 9);
    }

    [Fact]
    public void Add_Works_WhenInitialCapacityExceedsMaximumPooledBufferSize()
    {
        // Arrange
        var maxPooledBufferSize = 1024;
        var capacity = maxPooledBufferSize + 1;
        using var list = new PooledList<byte>(capacity, maxPooledBufferSize);
        var expected = Enumerable.Range(0, capacity).Select(i => (byte)i).ToArray();

        // Act
        foreach (var i in expected)
            list.Add(i);

        // Assert
        Assert.Equal(expected.Length, list.Count);
        Assert.Equal(expected, list.ToArray());
    }

    [Fact]
    public void Add_Works_WhenResizedCapacityExceedsMaximumPooledBufferSize()
    {
        // Arrange
        var maxPooledBufferSize = 128;
        using var list = new PooledList<byte>(1, maxPooledBufferSize);
        var expected = Enumerable.Range(0, maxPooledBufferSize + 10).Select(i => (byte)i).ToArray();

        // Act
        foreach (var i in expected)
            list.Add(i);

        // Assert
        Assert.Equal(expected.Length, list.Count);
        Assert.Equal(expected, list.ToArray());
    }

    [Fact]
    public void Dispose_DoesNotThrow_WithMaximumPooledBufferSize()
    {
        // Arrange
        var maxPooledBufferSize = 128;

        // Case 1: Under the limit
        var listUnder = new PooledList<byte>(1, maxPooledBufferSize);
        listUnder.Add(1);

        // Case 2: Over the limit by initial capacity
        var listOverCapacity = new PooledList<byte>(maxPooledBufferSize + 1, maxPooledBufferSize);
        listOverCapacity.Add(1);

        // Case 3: Over the limit by resize
        var listOverResize = new PooledList<byte>(1, maxPooledBufferSize);
        for (int i = 0; i < maxPooledBufferSize + 1; i++)
            listOverResize.Add((byte)i);

        // Act & Assert
        var exception = Record.Exception(() =>
        {
            listUnder.Dispose();
            listOverCapacity.Dispose();
            listOverResize.Dispose();
        });

        Assert.Null(exception);
    }

    [Fact]
    public void LegacyConstructor_DoesNotUseBufferPool()
    {
        // Arrange
        var capacity = 1024 * 1024; // 1MB
        using var list = new PooledList<byte>(capacity);

        // Act
        list.Add(1);

        // Assert
        Assert.Single(list);
        Assert.Equal((byte)1, list[0]);
    }
}
