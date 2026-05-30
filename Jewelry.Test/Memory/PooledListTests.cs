using System;
using System.Linq;
using Jewelry.Memory;
using Xunit;

namespace Jewelry.Test.Memory;

public sealed class PooledListTests
{
    [Fact]
    public void 要素を追加すると件数が増加しで順序が保持される()
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
    public void 小さい初期容量でもで容量拡張後に全要素が保持される()
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
    public void スパン取得で件数分のデータを返す()
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
    public void 列挙子で順序通りに列挙できる()
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
    public void 破棄後に追加を呼び出すとオブジェクト破棄例外が送出される()
    {
        // Arrange
        var list = new PooledList<int>(1);
        list.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => list.Add(1));
    }

    [Fact]
    public void 破棄後にスパン取得を呼び出すとオブジェクト破棄例外が送出される()
    {
        // Arrange
        var list = new PooledList<int>(1);
        list.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => _ = list.AsSpan());
    }

    [Fact]
    public void 破棄後に列挙子取得を呼び出すとオブジェクト破棄例外が送出される()
    {
        // Arrange
        var list = new PooledList<int>(1);
        list.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => _ = list.ToArray());
    }

    [Fact]
    public void インデクサ取得で追加済み要素を正しく取得できる()
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
    public void インデクサ設定で既存要素の値が更新される()
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
    public void インデクサ取得で範囲外インデックスは例外が発生する()
    {
        // Arrange
        using var list = new PooledList<int>(1);
        list.Add(42);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => _ = list[-1]);
        Assert.Throws<ArgumentOutOfRangeException>(() => _ = list[1]);
    }

    [Fact]
    public void インデクサ設定で範囲外インデックスは例外が発生する()
    {
        // Arrange
        using var list = new PooledList<int>(1);
        list.Add(0);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => list[-1] = 1);
        Assert.Throws<ArgumentOutOfRangeException>(() => list[1] = 1);
    }

    [Fact]
    public void インデクサ取得で破棄後はオブジェクト破棄例外が発生する()
    {
        // Arrange
        var list = new PooledList<int>(1);
        list.Add(7);
        list.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => _ = list[0]);
    }

    [Fact]
    public void インデクサ設定で破棄後はオブジェクト破棄例外が発生する()
    {
        // Arrange
        var list = new PooledList<int>(1);
        list.Add(7);
        list.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => list[0] = 9);
    }

    [Fact]
    public void 最大プールバッファサイズを超える容量を要求しても正常に利用できる()
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
    public void リストが最大プールバッファサイズを超えて拡張された場合も正常に動作する()
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
    public void 最大プールバッファサイズ指定のリストを破棄しても例外が発生しない()
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
    public void 古いコンストラクタはバッファプールを使用しない()
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
